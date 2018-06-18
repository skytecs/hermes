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

            _logger.Info("Инициализация сервиса печати чеков Атол.");

            using (var factory = new FiscalPrinterFactory(_config))
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
            }
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
            using (var factory = new FiscalPrinterFactory(_config))
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
            }
        }

        public void PrintReceipt(Receipt receipt)
        {
            Check.NotNull(receipt, nameof(receipt));
            Check.NotNull(receipt.Sum, nameof(receipt.Sum));

            _logger.Info("Печать чека начата.\nПроверка статуса смены.");
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
            _logger.Info("Снятие Z-отчета с закрытием смены.\nПроверка статуса текущей смены.");

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
}
