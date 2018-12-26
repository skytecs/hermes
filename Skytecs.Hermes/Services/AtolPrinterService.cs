using FprnM1C;
using Microsoft.Extensions.Logging;
using Skytecs.Hermes.Models;
using Skytecs.Hermes.Services;
using Skytecs.Hermes.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Runtime.Serialization;

namespace Skytecs.Hermes.Services
{
    public class AtolPrinterService : IFiscalPrinterService
    {
        private static object _lock = new object();

        private readonly ILogger<AtolPrinterService> _logger;
        private readonly ISessionStorage _sessionStorage;
        private readonly IOptions<FiscalPrinterSettings> _config;

        public AtolPrinterService(ILogger<AtolPrinterService> logger, ISessionStorage sessionStorage, IOptions<FiscalPrinterSettings> config)
        {
            Check.NotNull(logger, nameof(logger));
            Check.NotNull(config, nameof(config));
            Check.NotNull(sessionStorage, nameof(sessionStorage));

            _logger = logger;
            _sessionStorage = sessionStorage;
            _config = config;
        }

        public void CheckConnection()
        {
            using (var factory = new FiscalPrinterFactory(_config))
            {
                var printer = factory.GetPrinter();
                if (printer.GetStatus() != 0)
                {
                    throw new InvalidOperationException($"Неверное состояние ККМ: {printer.GetLastError()}");
                }
            }
        }

        public void OpenSession(int cashierId, string cashierName)
        {
            lock (_lock)
            {
                using (var atolPrinter = new AtolWrapper(_logger, _config))
                {
                    atolPrinter.Open();

                    CheckShiftIsClosed(atolPrinter);

                    _logger.Info($"Открытие смены для кассира {cashierId} ({cashierName}).");

                    var session = new CashierSession(cashierId, cashierName);
                    session.SessionStart = DateTime.Now;

                    _sessionStorage.Set(session);

                    _logger.Info("Открытие смены.");

                    atolPrinter.ExecuteCommand(new OpenShift
                    {
                        Operator = GetOperator()
                    });
                }
            }
        }

        public void PrintReceipt(Receipt receipt)
        {
            lock (_lock)
            {
                Check.NotNull(receipt, nameof(receipt));
                Check.NotNull(receipt.Sum, nameof(receipt.Sum));

                var servicesWithoutTaxation = receipt.Items.Where(x => x.TaxationType == null);
                if (servicesWithoutTaxation.Any())
                {
                    throw new InvalidOperationException($"Для некоторых услуг не указана 'Система налогообложения'.\n{String.Join("\n", servicesWithoutTaxation.Select(x => x.Description))}");
                }

                foreach (var group in receipt.Items.GroupBy(x => x.TaxationType))
                {
                    var subReceipt = new Receipt
                    {
                        Items = group.ToList(),
                        IsPaydByCard = receipt.IsPaydByCard
                    };

                    _logger.Info("Печать чека.");

                    using (var atolPrinter = new AtolWrapper(_logger, _config))
                    {
                        atolPrinter.Open();

                        CheckShiftIsOpend(atolPrinter);

                        var items = new List<PositionItem>();
                        foreach (var item in subReceipt.Items)
                        {
                            var position = new PositionItem
                            {
                                Name = item.Description,
                                Quantity = item.Quantity,
                                Price = (double)item.UnitPrice,
                                Amount = (double)item.Price,
                                PaymentObject = item.PaymentObjectType,
                            };

                            var taxType = VatType.None;
                            if (subReceipt.Items.First().TaxationType == TaxationType.Osn && item.TaxType.HasValue)
                            {
                                taxType = item.TaxType.Value;
                            }

                            position.Tax = new Tax { Type = taxType };

                            items.Add(position);
                        }

                        var payments = new List<Payment>();
                        payments.Add(new Payment
                        {
                            Sum = (double)subReceipt.Sum,
                            Type = subReceipt.IsPaydByCard ? PaymentType.Electronically : PaymentType.Cash
                        });

                        atolPrinter.ExecuteCommand(new PrintReceipt
                        {
                            Type = PrintReceiptCommand.Sell,
                            TaxationType = subReceipt.Items.First().TaxationType.Value,
                            Items = items,
                            Payments = payments,
                            Operator = GetOperator()

                        });
                    }
                }
            }
        }

        public void PrintRefund(Receipt receipt)
        {
            lock (_lock)
            {

                Check.NotNull(receipt, nameof(receipt));
                Check.NotNull(receipt.Sum, nameof(receipt.Sum));

                var servicesWithoutTaxation = receipt.Items.Where(x => x.TaxationType == null);
                if (servicesWithoutTaxation.Any())
                {
                    throw new InvalidOperationException($"Для некоторых услуг не указана 'Система налогообложения'.\n{String.Join("\n", servicesWithoutTaxation.Select(x => x.Description))}");
                }

                foreach (var group in receipt.Items.GroupBy(x => x.TaxationType))
                {
                    var subReceipt = new Receipt
                    {
                        Items = group.ToList(),
                        IsPaydByCard = receipt.IsPaydByCard
                    };

                    _logger.Info("Печать чека возврата.");

                    using (var atolPrinter = new AtolWrapper(_logger, _config))
                    {
                        atolPrinter.Open();

                        CheckShiftIsOpend(atolPrinter);

                        var items = new List<PositionItem>();
                        foreach (var item in subReceipt.Items)
                        {
                            var position = new PositionItem
                            {
                                Name = item.Description,
                                Quantity = item.Quantity,
                                Price = (double)item.UnitPrice,
                                Amount = (double)item.Price,
                            };

                            var taxType = VatType.None;
                            if (subReceipt.Items.First().TaxationType == TaxationType.Osn && item.TaxType.HasValue)
                            {
                                taxType = item.TaxType.Value;
                            }

                            position.Tax = new Tax { Type = taxType };

                            items.Add(position);
                        }


                        var payments = new List<Payment>();
                        payments.Add(new Payment
                        {
                            Sum = (double)subReceipt.Sum,
                            Type = subReceipt.IsPaydByCard ? PaymentType.Electronically : PaymentType.Cash
                        });

                        atolPrinter.ExecuteCommand(new PrintReceipt
                        {
                            Type = PrintReceiptCommand.SellReturn,
                            TaxationType = subReceipt.Items.First().TaxationType.Value,
                            Items = items,
                            Payments = payments,
                            Operator = GetOperator()
                        });
                    }
                }
            }
        }

        public void PrintCorrection(CorrectionReceipt receipt)
        {
            lock (_lock)
            {
                Check.NotNull(receipt, nameof(receipt));

                _logger.Info("Печать чека коррекции.");

                using (var atolPrinter = new AtolWrapper(_logger, _config))
                {
                    atolPrinter.Open();

                    CheckShiftIsOpend(atolPrinter);

                    var payments = new List<Payment>
                    {
                        new Payment
                        {
                            Sum = (double)receipt.Sum,
                            Type = receipt.IsPaydByCard ? PaymentType.Electronically : PaymentType.Cash
                        }
                    };

                    var taxes = new List<Tax>
                    {
                        new Tax
                        {
                            Type = receipt.TaxType
                        }
                    };

                    var command = new PrintCorrectionReceipt
                    {
                        Type = PrintCorrectionReceiptCommandType.SellCorrection,
                        Operator = GetOperator(),
                        Payments = payments,
                        Taxes = taxes,
                        TaxationType = receipt.TaxationType
                    };

                    var version = atolPrinter.GetFFDVersion();
                    if (version == "1.0.5" || version == "1.1")
                    {
                        if (version == "1.1")
                        {
                            if (!receipt.CorrectionType.HasValue)
                            {
                                throw new NullReferenceException($"Необходимо указать 'Тип коррекции'.");
                            }
                            if (String.IsNullOrEmpty(receipt.CorrectionBaseName))
                            {
                                throw new NullReferenceException($"Необходимо указать 'Описание коррекции'.");
                            }
                            if (!receipt.CorrectionBaseDate.HasValue)
                            {
                                throw new NullReferenceException($"Необходимо указать 'Дату документа'.");
                            }
                            if (String.IsNullOrEmpty(receipt.CorrectionBaseNumber))
                            {
                                throw new NullReferenceException($"Необходимо указать 'Номер документа'.");
                            }
                        }

                        command.CorrectionType = receipt.CorrectionType.Value;
                        command.CorrectionBaseName = receipt.CorrectionBaseName;
                        command.CorrectionBaseDate = receipt.CorrectionBaseDate?.ToString("yyyy.MM.dd");
                        command.CorrectionBaseNumber = receipt.CorrectionBaseNumber;
                    }

                    atolPrinter.ExecuteCommand(command);
                }
            }
        }


        public void PrintZReport()
        {
            lock (_lock)
            {
                _logger.Info("Снятие Z-отчета.");

                using (var atolPrinter = new AtolWrapper(_logger, _config))
                {
                    atolPrinter.Open();
                    atolPrinter.ExecuteCommand(new CloseShift
                    {
                        Operator = GetOperator()
                    });
                }

                _logger.Info("Печать Z-отчета завершена успешно.\nУдаление данных о текущей смене.");
                _sessionStorage.Remove();
                _logger.Info("Удаление данных о текущей смене завершено.");
            }
        }

        public void PrintXReport()
        {
            lock (_lock)
            {
                _logger.Info("Снятие X-отчета.");

                using (var atolPrinter = new AtolWrapper(_logger, _config))
                {
                    atolPrinter.Open();

                    CheckShiftIsOpend(atolPrinter);

                    atolPrinter.ExecuteCommand(new PrintXReport
                    {
                        Operator = GetOperator()
                    });
                }

                _logger.Info("Печать X-отчета завершена успешно.");
            }
        }

        private void CheckShiftIsOpend(AtolWrapper printer)
        {
            _logger.Info("Проверка состояния смены.");
            var response = printer.ExecuteCommand<GetShiftStatus, GetShiftStatusResponse>(new GetShiftStatus());
            var status = response.ShiftStatus.State;

            switch (status)
            {
                case ShiftState.Closed:
                    throw new InvalidOperationException("Смена закрыта.");
                case ShiftState.Expired:
                    throw new InvalidOperationException("Текущая смена привысила 24 часа.");
                case ShiftState.Opened:
                default:
                    _logger.Info("Смена открыта.");
                    return; //???
            }
        }

        private void CheckShiftIsClosed(AtolWrapper printer)
        {
            _logger.Info("Проверка состояния смены.");
            var response = printer.ExecuteCommand<GetShiftStatus, GetShiftStatusResponse>(new GetShiftStatus());
            var status = response.ShiftStatus.State;

            switch (status)
            {
                case ShiftState.Opened:
                    throw new InvalidOperationException("Смена открыта.");
                case ShiftState.Expired:
                    throw new InvalidOperationException("Текущая смена привысила 24 часа.");
                case ShiftState.Closed:
                default:
                    _logger.Info("Смена закрыта.");
                    return; //???
            }
        }


        private DeviceInfo GetDeviceInfo(AtolWrapper printer)
        {
            _logger.Info("Получение информаци об устройстве.");
            var response = printer.ExecuteCommand<GetDeviceInfo, GetDeviceInfoResponse>(new GetDeviceInfo());

            return response.DeviceInfo;
        }

        private Operator GetOperator()
        {
            var session = _sessionStorage.Get();
            if (session == null)
            {
                throw new Exception("Информация о смене не найдена.");
            }

            var casboxOperator = new Operator
            {
                Name = session.CashierName
            };

            return casboxOperator;
        }

        private int PrintItem(FprnM45 printer, RecItem item)
        {
            printer.Name = item.Description;
            printer.Price = (double)item.UnitPrice;
            printer.Quantity = item.Quantity;
            printer.Department = 0;
            // TaxTypeNumber - Номер налога:
            //     0 - Налог из секции
            //     1 - НДС 0%
            //     2 - НДС 10%
            //     3 - НДС 18%
            //     4 - НДС не облагается
            //     5 - НДС с расчётной ставкой 10%
            //     6 - НДС с расчётной ставкой 18%
            //_printer.TaxTypeNumber = 4;

            // рекомендуется рассчитывать в кассовом ПО цену со скидкой, а информацию по начисленным скидкам печатать нефискальной печатью и не передавать скидку в ККМ, поэтому код для начисления скидки закомментирован
            // driver.DiscountValue = 10;
            // // DiscountType - Тип скидки:
            // //     0 - суммовая
            // //     1 - процентная
            // driver.DiscountType = 0;
            return printer.Registration();
        }

        private int PrintRefundItem(FprnM45 printer, RecItem item)
        {
            printer.Name = item.Description;
            printer.Price = (double)item.UnitPrice;
            printer.Quantity = item.Quantity;
            // TaxTypeNumber - Номер налога:
            //     0 - Налог из секции
            //     1 - НДС 0%
            //     2 - НДС 10%
            //     3 - НДС 18%
            //     4 - НДС не облагается
            //     5 - НДС с расчётной ставкой 10%
            //     6 - НДС с расчётной ставкой 18%
            //_printer.TaxTypeNumber = 4;

            return printer.Return();
        }

        /// <summary>
        /// Закрываем открытй чек
        /// </summary>
        private void CloseCheck(FprnM45 printer)
        {
            if (printer.CheckState != 0 && printer.CancelCheck() != 0)
            {
                throw new InvalidOperationException($"Не удалось отменить печать чека. \n{printer.ResultCode} - { printer.ResultDescription}");
            }
        }

        private void CheckStatus(FprnM45 printer)
        {
            if (printer.ResultCode != 0)
            {
                throw new InvalidOperationException($"\n{printer.ResultCode} - { printer.ResultDescription}");
            }
        }



        private void SetCachier(FprnM45 printer, string name)
        {
            _logger.Info($"Задание имени кассира: {name}.");

            Check.NotEmpty(name, nameof(name));
            printer.AttrNumber = 1021;
            printer.AttrValue = name;
            if (printer.WriteAttribute() != 0)
            {
                throw new InvalidOperationException("Не удалось задать имя кассира.");
            }
        }
        // Применяемая система налогооблажения в чеке:
        //     ОСН - 1
        //     УСН доход - 2
        //     УСН доход-расход - 4
        //     ЕНВД - 8
        //     ЕСН - 16
        //     ПСН - 32
        //public void SetTaxing()
        //{
        //    printer.AttrNumber = 1055;
        //    printer.AttrValue = "2";
        //    printer.WriteAttribute();
        //    CheckStatus();
        //}

    }


    public class CorrectionReceipt
    {
        public decimal Sum { get; set; }
        public bool IsPaydByCard { get; set; }
        public VatType TaxType { get; set; }
        public TaxationType TaxationType { get; set; }
        public CorrectionType? CorrectionType { get; set; }
        public string CorrectionBaseName { get; set; }
        public DateTime? CorrectionBaseDate { get; set; }
        public string CorrectionBaseNumber { get; set; }
    }

    public class FiscalPrinterFactory : IDisposable
    {
        private FprnM45Class _printer;
        private int _port;
        private int _deviceId;

        public FiscalPrinterFactory(IOptions<FiscalPrinterSettings> config)
        {
            _port = config.Value.Port;
            _deviceId = config.Value.DeviceId;
        }

        public FiscalPrinterFactory(int port, int deviceId)
        {
            _port = port;
            _deviceId = deviceId;
        }

        public FprnM45Class GetPrinter()
        {
            _printer = new FprnM45Class()
            {
                PortNumber = _port,
                Model = _deviceId,
                DeviceEnabled = true
            };

            return _printer;
        }

        public void Dispose()
        {
            _printer.DeviceEnabled = false;
        }
    }



    #region Atol Data Contracts
    [DataContract]
    public abstract class FiscalPrinterCommand<TCommandType>
    {
        [DataMember(Name = "type")]
        public TCommandType Type { get; set; }

        [DataMember(Name = "operator")]
        public Operator Operator { get; set; }
    }

    [DataContract]
    public class Operator
    {

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "vatin")]
        public string Vatin { get; set; }
    }

    [DataContract]
    public class OpenShift : FiscalPrinterCommand<OpenShiftCommandType>
    {
    }

    [DataContract]
    public enum OpenShiftCommandType
    {
        [EnumMember(Value = "openShift")] OpenShift
    }

    [DataContract]
    public class FiscalParams
    {

        [DataMember(Name = "fiscalDocumentDateTime")]
        public DateTime FiscalDocumentDateTime { get; set; }

        [DataMember(Name = "fiscalDocumentNumber")]
        public int FiscalDocumentNumber { get; set; }

        [DataMember(Name = "fiscalDocumentSign")]
        public string FiscalDocumentSign { get; set; }

        [DataMember(Name = "fnNumber")]
        public string FnNumber { get; set; }

        [DataMember(Name = "registrationNumber")]
        public string RegistrationNumber { get; set; }

        [DataMember(Name = "shiftNumber")]
        public int ShiftNumber { get; set; }

        [DataMember(Name = "fnsUrl")]
        public string FnsUrl { get; set; }
    }

    [DataContract]
    public class Warnings
    {

        [DataMember(Name = "notPrinted")]
        public bool NotPrinted { get; set; }
    }

    [DataContract]
    public class CommandResponse
    {

        [DataMember(Name = "fiscalParams")]
        public FiscalParams FiscalParams { get; set; }

        [DataMember(Name = "warnings")]
        public Warnings Warnings { get; set; }
    }

    [DataContract]
    public class CloseShift : FiscalPrinterCommand<CloseShiftCommandType>
    {
    }

    [DataContract]
    public enum CloseShiftCommandType
    {
        [EnumMember(Value = "closeShift")] CloseShift
    }

    [DataContract]
    public class Tax
    {
        [DataMember(Name = "type")]
        public VatType Type { get; set; }
    }

    [DataContract]
    public enum VatType
    {
        [EnumMember(Value = "none")] None = 1,
        [EnumMember(Value = "vat0")] Vat0 = 2,
        [EnumMember(Value = "vat10")] Vat10 = 3,
        [EnumMember(Value = "vat18")] Vat18 = 4,
        [EnumMember(Value = "vat110")] Vat110 = 5,
        [EnumMember(Value = "vat118")] Vat118 = 6,
        [EnumMember(Value = "vat20")] vat20 = 7,
        [EnumMember(Value = "vat120")] vat120 = 8

    }

    [DataContract]
    public class PayingAgent
    {
        [DataMember(Name = "operation")]
        public string Operation { get; set; }

        [DataMember(Name = "phones")]
        public IList<string> Phones { get; set; }
    }

    [DataContract]
    public class ReceivePaymentsOperator
    {

        [DataMember(Name = "phones")]
        public IList<string> Phones { get; set; }
    }

    [DataContract]
    public class MoneyTransferOperator
    {

        [DataMember(Name = "phones")]
        public IList<string> Phones { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "address")]
        public string Address { get; set; }

        [DataMember(Name = "vatin")]
        public string Vatin { get; set; }
    }

    [DataContract]
    public class AgentInfo
    {

        [DataMember(Name = "agents")]
        public IList<string> Agents { get; set; }

        [DataMember(Name = "payingAgent")]
        public PayingAgent PayingAgent { get; set; }

        [DataMember(Name = "receivePaymentsOperator")]
        public ReceivePaymentsOperator ReceivePaymentsOperator { get; set; }

        [DataMember(Name = "moneyTransferOperator")]
        public MoneyTransferOperator MoneyTransferOperator { get; set; }
    }

    [DataContract]
    public class SupplierInfo
    {

        [DataMember(Name = "phones")]
        public IList<string> Phones { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "vatin")]
        public string Vatin { get; set; }
    }

    [DataContract]
    public abstract class Item<TItemType>
    {

        [DataMember(Name = "type")]
        public TItemType Type { get; set; }
    }

    [DataContract]
    public class PositionItem : Item<PositionItemType>
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "price")]
        public double Price { get; set; }

        [DataMember(Name = "quantity")]
        public double Quantity { get; set; }

        [DataMember(Name = "amount")]
        public double Amount { get; set; }

        [DataMember(Name = "infoDiscountAmount")]
        public double? InfoDiscountAmount { get; set; }

        [DataMember(Name = "department")]
        public int? Department { get; set; }

        [DataMember(Name = "measurementUnit")]
        public string MeasurementUnit { get; set; }

        [DataMember(Name = "paymentMethod")]
        public PaymentMethod? PaymentMethod { get; set; }

        [DataMember(Name = "paymentObject")]
        public PaymentObject? PaymentObject { get; set; }

        [DataMember(Name = "nomenclatureCode")]
        public object NomenclatureCode { get; set; }

        [DataMember(Name = "tax")]
        public Tax Tax { get; set; }

        [DataMember(Name = "agentInfo")]
        public AgentInfo AgentInfo { get; set; }

        [DataMember(Name = "supplierInfo")]
        public SupplierInfo SupplierInfo { get; set; }
    }

    [DataContract]
    public class TextItem : Item<TextItemType>
    {
        [DataMember(Name = "text")]
        public string Text { get; set; }

        [DataMember(Name = "alignment")]
        public Alignment? Alignment { get; set; }

        [DataMember(Name = "font")]
        public int? Font { get; set; }

        [DataMember(Name = "doubleWidth")]
        public bool? DoubleWidth { get; set; }

        [DataMember(Name = "doubleHeight")]
        public bool? DoubleHeight { get; set; }
    }

    [DataContract]
    public class BarcodeItem : Item<BarcodeItemType>
    {
        [DataMember(Name = "barcode")]
        public string Barcode { get; set; }

        [DataMember(Name = "barcodeType")]
        public BarcodeType BarcodeType { get; set; }

        [DataMember(Name = "alignment")]
        public Alignment? Alignment { get; set; }

        [DataMember(Name = "scale")]
        public int? Scale { get; set; }

        [DataMember(Name = "printText")]
        public bool? PrintText { get; set; }
    }

    [DataContract]
    public enum BarcodeType
    {
        EAN8,
        EAN13,
        CODE39,
        QR,
        AZTEC
    }

    public enum PositionItemType
    {
        Position
    }

    public enum TextItemType
    {
        Text
    }

    public enum BarcodeItemType
    {
        Barcode
    }

    [DataContract]
    public enum Alignment
    {
        Left,
        Right,
        Center
    }

    [DataContract]
    public enum PaymentMethod
    {
        [DataMember(Name = "fullPrepayment")] FullPrepayment = 1,
        [DataMember(Name = "prepayment")] Prepayment = 2,
        [DataMember(Name = "advance")] Advance = 3,
        [DataMember(Name = "fullPayment")] FullPayment = 4,
        [DataMember(Name = "partialPayment")] PartialPayment = 5,
        [DataMember(Name = "credit")] Credit = 6,
        [DataMember(Name = "creditPayment")] CreditPayment = 7
    }

    [DataContract]
    public enum PaymentObject
    {
        [DataMember(Name = "commodity")] Commodity = 1,
        [DataMember(Name = "excise")] Excise = 2,
        [DataMember(Name = "lob")] Job = 3,
        [DataMember(Name = "service")] Service = 4,
        [DataMember(Name = "gamblingBet")] GamblingBet = 5,
        [DataMember(Name = "gamblingPrize")] GamblingPrize = 6,
        [DataMember(Name = "lottery")] Lottery = 7,
        [DataMember(Name = "lotteryPrize")] LotteryPrize = 8,
        [DataMember(Name = "intellectualActivity")] IntellectualActivity = 9,
        [DataMember(Name = "payment")] Payment = 10,
        [DataMember(Name = "agentCommission")] AgentCommission = 11,
        [DataMember(Name = "composite")] Composite = 12,
        [DataMember(Name = "another")] Another = 13
    }

    [DataContract]
    public class Payment
    {

        [DataMember(Name = "type")]
        public PaymentType Type { get; set; }

        [DataMember(Name = "sum")]
        public double Sum { get; set; }
    }

    [DataContract]
    public enum PaymentType
    {
        [EnumMember(Value = "cash")] Cash = 0,
        [EnumMember(Value = "electronically")] Electronically = 1,
        [EnumMember(Value = "prepaid")] Prepaid = 2,
        [EnumMember(Value = "credit")] Credit = 3,
        [EnumMember(Value = "other")] Other = 4,
    }

    [DataContract]
    public class PrintReceipt : FiscalPrinterCommand<PrintReceiptCommand>
    {
        [DataMember(Name = "taxationType")]
        public TaxationType TaxationType { get; set; }

        [DataMember(Name = "items")]
        public ICollection<PositionItem> Items { get; set; }

        [DataMember(Name = "payments")]
        public ICollection<Payment> Payments { get; set; }

        [DataMember(Name = "total")]
        public double? Total { get; set; }

        [DataMember(Name = "electronically")]
        public bool Electronically { get; set; }

        [DataMember(Name="clientInfo")]
        public ClientInfo ClientInfo { get; set; }
    }

    [DataContract]
    public class ClientInfo
    {
        [DataMember(Name="emailOrPhone")]
        public string EmailOrPhone { get; set; }
    }

    [DataContract]
    public enum PrintReceiptCommand
    {
        [EnumMember(Value = "sell")] Sell,
        [EnumMember(Value = "sellReturn")] SellReturn,
        [EnumMember(Value = "buy")] Buy,
        [EnumMember(Value = "buyReturn")] BuyReturn
    }

    [DataContract]
    public enum TaxationType
    {
        [EnumMember(Value = "osn")] Osn = 1,
        [EnumMember(Value = "usnIncome")] UsnIncome = 2,
        [EnumMember(Value = "usnIncomeOutcome")] UsnIncomeOutcome = 3,
        [EnumMember(Value = "envd")] Envd = 4,
        [EnumMember(Value = "esn")] Esn = 5,
        [EnumMember(Value = "patent")] Patent = 6
    }

    [DataContract]
    public class PrintXReport : FiscalPrinterCommand<PrintXReportCommandType>
    {
    }

    public enum PrintXReportCommandType
    {
        [EnumMember(Value = "reportX")] ReportX
    }


    [DataContract]
    public class PrintCorrectionReceipt : FiscalPrinterCommand<PrintCorrectionReceiptCommandType>
    {
        [DataMember(Name = "correctionType")]
        public CorrectionType? CorrectionType { get; set; }

        [DataMember(Name = "correctionBaseDate")]
        public string CorrectionBaseDate { get; set; }

        [DataMember(Name = "correctionBaseNumber")]
        public string CorrectionBaseNumber { get; set; }

        [DataMember(Name = "correctionBaseName")]
        public string CorrectionBaseName { get; set; }

        [DataMember(Name = "payments")]
        public IList<Payment> Payments { get; set; }

        [DataMember(Name = "taxes")]
        public IList<Tax> Taxes { get; set; }

        [DataMember(Name = "taxationType")]
        public TaxationType TaxationType { get; set; }
    }


    public enum PrintCorrectionReceiptCommandType
    {
        [EnumMember(Value = "sellCorrection")] SellCorrection,
        [EnumMember(Value = "buyCorrection")] BuyCorrection,
    }

    public enum CorrectionType
    {
        [EnumMember(Value = "self")]
        Self,
        [EnumMember(Value = "instruction")]
        Instruction
    }


    public class GetShiftStatus : FiscalPrinterCommand<GetShiftStatusCommandType>
    {
    }
    public enum GetShiftStatusCommandType
    {
        [EnumMember(Value = "getShiftStatus")] GetShiftStatus,
    }
    public class GetShiftStatusResponse
    {
        [DataMember(Name = "shiftStatus")]
        public ShiftStatus ShiftStatus { get; set; }

    }
    [DataContract]
    public class ShiftStatus
    {
        [DataMember(Name = "expiredTime")]
        public DateTime ExpiredTime { get; set; }

        [DataMember(Name = "number")]
        public string Number { get; set; }

        [DataMember(Name = "state")]
        public ShiftState State { get; set; }
    }
    public enum ShiftState
    {
        [EnumMember(Value = "closed")] Closed,
        [EnumMember(Value = "opened")] Opened,
        [EnumMember(Value = "expired")] Expired,
    }


    [DataContract]
    public class GetDeviceInfo : FiscalPrinterCommand<GetDeviceInfoCommandType>
    {
    }
    public enum GetDeviceInfoCommandType
    {
        [EnumMember(Value = "getDeviceInfo")] GetDeviceInfo,
    }
    [DataContract]
    public class GetDeviceInfoResponse
    {
        [DataMember(Name = "deviceInfo")]
        public DeviceInfo DeviceInfo { get; set; }
    }
    [DataContract]
    public class DeviceInfo
    {
        [DataMember(Name = "firmwareVersion")]
        public string FirmwareVersion { get; set; }

        [DataMember(Name = "model")]
        public string Model { get; set; }

        [DataMember(Name = "modelName")]
        public string ModelName { get; set; }

        [DataMember(Name = "receiptLineLength")]
        public string ReceiptLineLength { get; set; }

        [DataMember(Name = "receiptLineLengthPix")]
        public string ReceiptLineLengthPix { get; set; }

        [DataMember(Name = "serial")]
        public string Serial { get; set; }
    }


    [DataContract]
    public class ContinuePrint : FiscalPrinterCommand<ContinuePrintCommandType>
    {
    }
    public enum ContinuePrintCommandType
    {
        [EnumMember(Value = "continuePrint")] ContinuePrint,
    }


    //getDeviceStatus


    #endregion
}

