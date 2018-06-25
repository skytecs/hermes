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

            //_logger.Info("Инициализация сервиса печати чеков Атол.");

            //using (var atolPrinter = new AtolWrapper(_config))
            //{
            //    Console.WriteLine(atolPrinter.GetSettings());

            //    atolPrinter.Open();

            //    atolPrinter.IsOpened();

            //}

            /*using (var factory = new FiscalPrinterFactory(_config))
            {
                var printer = factory.GetPrinter();

                _logger.Info("Проверка состояния ККМ.");
                if (printer.GetStatus() != 0)
                {
                    throw new InvalidOperationException($"Неверное состояние ККМ: {printer.GetLastError()}");
                }
                _logger.Info("Проверка состояния ККМ завершена успешно.");

                // если есть открытый чек, то отменяем его
                if (printer.CheckState != 0)
                {
                    _logger.Info("Отмена открытого чека.");
                    if (printer.CancelCheck() != 0)
                    {
                        throw new InvalidOperationException($"Не удалось отменить окрытый чек.");
                    }
                }

                if (printer.ResetMode() != 0)
                {
                    throw new InvalidOperationException($"Не удалось перейти в режим выбора. \n{printer.ResultCode} - { printer.ResultDescription}");
                }
                _logger.Info("Инициализация сервиса печати чеков Атол завершена.");
            }*/
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
            using (var atolPrinter = new AtolWrapper(_config))
            {
                atolPrinter.Open();

                _logger.Info($"Попытка открытия смены для кассира {cashierId} ({cashierName}).\nПроверка статуса текущей смены.");
                //if (atolPrinter.SessionOpened)
                //{
                //}

                var session = new CashierSession(cashierId, cashierName);
                //atolPrinter.SetParam(1021, cashierName);
                session.SessionStart = DateTime.Now;

                _sessionStorage.Set(session);

                _logger.Info("Открытие смены.");

                atolPrinter.ExecuteCommand<OpenShift>(new OpenShift());
            }

            /*using (var factory = new FiscalPrinterFactory(_config))
            {
                var printer = factory.GetPrinter();

                _logger.Info($"Попытка открытия смены для кассира {cashierId} ({cashierName}).\nПроверка статуса текущей смены.");
                if (printer.SessionOpened)
                {
                    _logger.Info("Смена открыта.");

                    if (printer.SessionExceedLimit)
                    {
                        throw new InvalidOperationException("Текущая смена привысила 24 часа.");
                    }

                    _logger.Info("Получение информации по текущей смене.");
                    var session = _sessionStorage.Get();
                    if (session == null)
                    {
                        throw new InvalidOperationException("Информация по текущей смене не найдена.");
                    }

                    _logger.Info($"Информация по текущей смене получена. Кассир {session.CashierId} ({session.CashierName}). Дата открытия: {session.SessionStart.ToString("dd.mm.yy hh:MM:ss")} ");

                    if (session.CashierId == cashierId)
                    {
                        throw new InvalidOperationException("Смена для запрошенного кассира уже открыта.");
                    }

                    throw new InvalidOperationException("Смена открыта для другого кассира.");
                }
                else
                {
                    _logger.Info("Смена закрыта.");

                    var session = new CashierSession(cashierId, cashierName);

                    _logger.Info("Переход в режим 1 для открытия смены.");

                    printer.Mode = 1;
                    printer.Password = session.CashierId.ToString();
                    if (printer.SetMode() != 0)
                    {
                        throw new InvalidOperationException($"Не удалось перейти в режим 1. \n{printer.ResultCode} - { printer.ResultDescription}");
                    }

                    SetCachier(printer, session.CashierName);

                    session.SessionStart = DateTime.Now;

                    _sessionStorage.Set(session);

                    _logger.Info("Открытие смены.");

                    if (printer.OpenSession() != 0)
                    {
                        _sessionStorage.Remove();
                        throw new InvalidOperationException($"Не удалось открыть сессию для кассира {session.CashierId} ({session.CashierName}).  \n{printer.ResultCode} - { printer.ResultDescription}");
                    }
                }

                if (printer.ResetMode() != 0)
                {
                    throw new InvalidOperationException($"Не удалось перейти в режим выбора. \n{printer.ResultCode} - { printer.ResultDescription}");
                }
            }*/
        }

        public void PrintReceipt(Receipt receipt)
        {
            Check.NotNull(receipt, nameof(receipt));
            Check.NotNull(receipt.Sum, nameof(receipt.Sum));

            _logger.Info("Печать чека");

            using (var atolPrinter = new AtolWrapper(_config))
            {
                atolPrinter.Open();

                var items = new List<object>();
                foreach (var item in receipt.Items)
                {
                    items.Add(new PositionItem
                    {
                        Name = item.Description,
                        Quantity = item.Quantity,
                        Price = (double)item.Price,
                        Amount = (double)item.Price * item.Quantity,
                        //TODO: Tax = new Tax { Type = item.TaxType } 
                        Tax = new Tax { Type = VatType.None }
                    });
                }

                var payments = new List<Payment>();
                payments.Add(new Payment
                {
                    Sum = (double)receipt.Sum,
                    Type = receipt.IsPaydByCard ? PaymentType.Electronically : PaymentType.Cash
                });

                atolPrinter.ExecuteCommand(new PrintReceipt
                {
                    Type = PrintReceiptCommand.Sell,
                    //TaxationType = TaxationType.UsnIncomeOutcome,
                    Items = items,
                    Payments = payments,
                    Operator = GetOperator()
                });
            }

            /*
            using (var factory = new FiscalPrinterFactory(_config))
            {
                var printer = factory.GetPrinter();

                if (!printer.SessionOpened)
                {
                    throw new InvalidOperationException("Смена закрыта.");
                }
                if (printer.SessionExceedLimit)
                {
                    throw new InvalidOperationException("Текущая смена привысила 24 часа.");
                }

                _logger.Info("Смена открыта.\nПолучение информации по текущей смене.");

                var session = _sessionStorage.Get();
                if (session == null)
                {
                    throw new InvalidOperationException("Информация по текущей смене не найдена");
                }
                _logger.Info($"Информация по текущей смене получена. Кассир {session.CashierId} ({session.CashierName}). Дата открытия: {session.SessionStart.ToString("dd.mm.yy hh:MM:ss")} ");

                SetCachier(printer, session.CashierName);

                CloseCheck(printer);

                printer.Mode = 1;
                if (printer.SetMode() != 0)
                {
                    throw new InvalidOperationException($"Не удалось перейти в режим 1. \n{printer.ResultCode} - { printer.ResultDescription}");
                }

                _logger.Info("Создание чека.");

                if (printer.NewDocument() != 0)
                {
                    throw new InvalidOperationException($"Не удалось создать чек. \n{printer.ResultCode} - { printer.ResultDescription}");
                }

                _logger.Info("Чек создан успешно.\nОткрытие чека.");

                printer.CheckType = 1;
                printer.CheckMode = 1;
                if (printer.OpenCheck() != 0)
                {
                    throw new InvalidOperationException($"Не удалось открыть чек. \n{printer.ResultCode} - { printer.ResultDescription}");
                }

                _logger.Info("Чек открыт успешно.\nПечать позиций чека.");

                foreach (var item in receipt.Items)
                {
                    if (PrintItem(printer, item) != 0)
                    {
                        throw new InvalidOperationException($"Не удалось зарегистрировать позицию \"{item.Description}\" (цена: {item.Price}, количество: {item.Quantity}). \n{printer.ResultCode} - { printer.ResultDescription}");
                    }
                }

                _logger.Info("Печать позиций чека завершена.");

                printer.TypeClose = receipt.IsPaydByCard ? 1 : 0;
                printer.Summ = (double)receipt.Sum;

                _logger.Info($"Регистрация платежа на сумму {printer.Summ}.");

                if (printer.Payment() != 0)
                {
                    throw new InvalidOperationException($"Не удалось зарегистрировать платеж (сумма - {printer.Summ}). \n{printer.ResultCode} - { printer.ResultDescription}");
                }

                _logger.Info("Платеж зарегистрирован.\nЗакрытие чека.");

                if (printer.CloseCheck() != 0)
                {
                    throw new InvalidOperationException($"Не удалось закрыть чек. \n{printer.ResultCode} - { printer.ResultDescription}");
                }

                _logger.Info("Чек закрыт.\nПечать чека завершена успешно.");

                if (printer.ResetMode() != 0)
                {
                    throw new InvalidOperationException($"Не удалось перейти в режим выбора. \n{printer.ResultCode} - { printer.ResultDescription}");
                }
            }
            */
        }

        public void PrintRefund(Receipt receipt)
        {
            Check.NotNull(receipt, nameof(receipt));
            Check.NotNull(receipt.Sum, nameof(receipt.Sum));

            _logger.Info("Печать чека начата.\nПроверка статуса смены.");

            using (var factory = new FiscalPrinterFactory(_config))
            {
                var printer = factory.GetPrinter();


                if (!printer.SessionOpened)
                {
                    throw new InvalidOperationException("Не удалось напечатать чек. Смена закрыта.");
                }

                _logger.Info("Смена открыта.\nПолучение информации по текущей смене.");

                var session = _sessionStorage.Get();
                if (session == null)
                {
                    throw new InvalidOperationException("Информация по текущей смене не найдена.");
                }

                _logger.Info($"Информация по текущей смене получена. Кассир {session.CashierId} ({session.CashierName}). Дата открытия: {session.SessionStart.ToString("dd.mm.yy hh:MM:ss")} ");

                SetCachier(printer, session.CashierName);

                CloseCheck(printer);

                printer.Mode = 1;
                if (printer.SetMode() != 0)
                {
                    throw new InvalidOperationException($"Не удалось перейти в режим 1. \n{printer.ResultCode} - { printer.ResultDescription}");
                }

                _logger.Info("Создание чека.");

                if (printer.NewDocument() != 0)
                {
                    throw new InvalidOperationException($"Не удалось создать чек. \n{printer.ResultCode} - { printer.ResultDescription}");
                }

                _logger.Info("Чек создан успешно.\nОткрытие чека.");

                printer.CheckType = 2;
                printer.CheckMode = 1;
                if (printer.OpenCheck() != 0)
                {
                    throw new InvalidOperationException($"Не удалось открыть чек. \n{printer.ResultCode} - { printer.ResultDescription}");
                }

                _logger.Info("Чек открыт успешно.");

                if (printer.WriteAttribute() != 0)
                {
                    throw new InvalidOperationException($"Не удалось указать систему налогообложения. \n{printer.ResultCode} - { printer.ResultDescription}");
                }

                _logger.Info("Система налогообложения задана.\nПечать позиций чека");
                foreach (var item in receipt.Items)
                {
                    if (PrintRefundItem(printer, item) != 0)
                    {
                        throw new InvalidOperationException($"Не удалось зарегистрировать позицию \"{item.Description}\" (цена: {item.Price}, количество: {item.Quantity}). \n{printer.ResultCode} - { printer.ResultDescription}");
                    }
                }

                _logger.Info("Печать позиций чека завершена.");

                printer.TypeClose = receipt.IsPaydByCard ? 1 : 0;
                printer.Summ = (double)receipt.Sum;
                printer.Destination = 0;

                _logger.Info("Закрытие чека.");

                if (printer.CloseCheck() != 0)
                {
                    throw new InvalidOperationException($"Не удалось закрыть чек. \n{printer.ResultCode} - { printer.ResultDescription}");
                }

                _logger.Info("Чек закрыт.\nПечать чека завершена успешно.");

                if (printer.ResetMode() != 0)
                {
                    throw new InvalidOperationException($"Не удалось перейти в режим выбора. \n{printer.ResultCode} - { printer.ResultDescription}");
                }
            }
        }

        public void PrintZReport()
        {
            _logger.Info("Снятие Z-отчета.");

            using (var atolPrinter = new AtolWrapper(_config))
            {
                atolPrinter.Open();

                atolPrinter.ExecuteCommand(new CloseShift());

                _sessionStorage.Remove();
            }

            /*
            using (var factory = new FiscalPrinterFactory(_config))
            {
                var printer = factory.GetPrinter();

                if (!printer.SessionOpened)
                {
                    _logger.Info("Текущая смена уже закрыта.");
                    _sessionStorage.Remove();
                    return;
                }
                _logger.Info("Текущая смены открыта.\nПолучение информации по текущей смене.");

                var session = _sessionStorage.Get();
                if (session != null)
                {
                    _logger.Info($"Информация по текущей смене получена. Кассир {session.CashierId} ({session.CashierName}). Дата открытия: {session.SessionStart.ToString("dd.mm.yy hh:MM:ss")} ");

                    SetCachier(printer, session.CashierName);
                }

                _logger.Info("Переход в режим 3 для снятия z-отчета.");

                printer.Password = "30";
                printer.Mode = 3;

                if (printer.SetMode() != 0)
                {
                    throw new InvalidOperationException($"Не удалось перейти в режим 3. \n{printer.ResultCode} - { printer.ResultDescription}");
                }

                _logger.Info("Печать z-отчета");
                printer.ReportType = 1;

                if (printer.Report() != 0)
                {
                    throw new InvalidOperationException($"Не удалось сформировать отчет типа 3. \n{printer.ResultCode} - { printer.ResultDescription}");
                }
                _logger.Info("Печать z-отчета завершена успешно.\nЗакрытие текущей смены.");
                _sessionStorage.Remove();
                _logger.Info("Текущая смена закрыта.");

                if (printer.ResetMode() != 0)
                {
                    throw new InvalidOperationException($"Не удалось перейти в режим выбора. \n{printer.ResultCode} - { printer.ResultDescription}");
                }
            }
            */
        }

        public void PrintXReport()
        {
            _logger.Info("Снятие X-отчета с закрытием смены.\nПроверка статуса текущей смены.");
            using (var factory = new FiscalPrinterFactory(_config))
            {
                var printer = factory.GetPrinter();

                if (!printer.SessionOpened)
                {
                    throw new InvalidOperationException("Текущая смены уже закрыта.");
                }

                _logger.Info("Текущая смены открыта.\nПолучение информации по текущей смене.");

                var session = _sessionStorage.Get();
                if (session == null)
                {
                    throw new InvalidOperationException("Информация по текущей смене не найдена.");
                }

                _logger.Info($"Информация по текущей смене получена. Кассир {session.CashierId} ({session.CashierName}). Дата открытия: {session.SessionStart.ToString("dd.mm.yy hh:MM:ss")} ");

                SetCachier(printer, session.CashierName);

                _logger.Info("Переход в режим 2 для снятия X-отчета.");
                // устанавливаем пароль администратора ККМ
                printer.Password = "29";
                // входим в режим отчетов без гашения
                printer.Mode = 2;
                if (printer.SetMode() != 0)
                {
                    throw new InvalidOperationException($"Не удалось перейти в режим 2. \n{printer.ResultCode} - { printer.ResultDescription}");
                }

                _logger.Info("Печать X-отчета.");
                printer.ReportType = 2;
                if (printer.Report() != 0)
                {
                    throw new InvalidOperationException($"Не удалось сформировать отчет типа 2. \n{printer.ResultCode} - { printer.ResultDescription}");
                }

                _logger.Info("Печать X-отчета завершена успешно.");

                if (printer.ResetMode() != 0)
                {
                    throw new InvalidOperationException($"Не удалось перейти в режим выбора. \n{printer.ResultCode} - { printer.ResultDescription}");
                }
            }
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
        [EnumMember(Value = "none")] None,
        [EnumMember(Value = "vat0")] Vat0,
        [EnumMember(Value = "vat10")] Vat10,
        [EnumMember(Value = "vat18")] Vat18,
        [EnumMember(Value = "vat110")] Vat110,
        [EnumMember(Value = "vat118")] Vat118
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
    public enum PaymentObject
    {
        FullPrepayment,
        Prepayment,
        Advance,
        FullPayment,
        PartialPayment,
        Credit,
        CreditPayment
    }

    [DataContract]
    public enum PaymentMethod
    {
        Commodity,
        Excise,
        Job,
        Service,
        GamblingBet,
        GamblingPrize,
        Lottery,
        LotteryPrize,
        IntellectualActivity,
        Payment,
        AgentCommission,
        Composite,
        Another
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
        public ICollection<object> Items { get; set; }

        [DataMember(Name = "payments")]
        public ICollection<Payment> Payments { get; set; }

        [DataMember(Name = "total")]
        public double? Total { get; set; }
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
        [EnumMember(Value = "osn")] Osn,
        [EnumMember(Value = "usnIncome")] UsnIncome,
        [EnumMember(Value = "usnIncomeOutcome")] UsnIncomeOutcome,
        [EnumMember(Value = "envd")] Envd,
        [EnumMember(Value = "esn")] Esn,
        [EnumMember(Value = "patent")] Patent
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
        [DataMember(Name = "taxationType")]
        public TaxationType TaxationType { get; set; }

        [DataMember(Name = "correctionType")]
        public CorrectionType CorrectionType { get; set; }

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

    #endregion
}
