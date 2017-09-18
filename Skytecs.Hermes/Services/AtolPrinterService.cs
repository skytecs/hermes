using FprnM1C;
using Microsoft.Extensions.Logging;
using Skytecs.Hermes.Models;
using Skytecs.Hermes.Services;
using Skytecs.Hermes.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Services
{
    public class AtolPrinterService : IFiscalPrinterService
    {
        private readonly FprnM45Class _printer;
        private readonly ILogger<AtolPrinterService> _logger;
        private readonly ISessionStorage _sessionStorage;

        public AtolPrinterService(ILogger<AtolPrinterService> logger, ISessionStorage sessionStorage)
        {
            Check.NotNull(logger, nameof(logger));
            Check.NotNull(sessionStorage, nameof(sessionStorage));

            _logger = logger;
            _sessionStorage = sessionStorage;

            _logger.Info("Инициализация сервиса печати чеков Атол");

            _printer = new FprnM45Class {DeviceEnabled = true};

            _logger.Info("Проверка состояния ККМ");
            if (_printer.GetStatus() != 0)
            {
                throw new InvalidOperationException($"Неверное состояние ККМ: {_printer.GetLastError()}");
            }
            _logger.Info("Проверка состояния ККМ завершена успешно");

            // если есть открытый чек, то отменяем его
            if (_printer.CheckState != 0)
            {
                _logger.Info("Отмена открытого чека.");
                if (_printer.CancelCheck() != 0)
                {
                    throw new InvalidOperationException($"Не удалось отменить окрытый чек.");
                }
            }

            if (_printer.ResetMode() != 0)
            {
                throw new InvalidOperationException($"Не удалось перейти в режим выбора. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }
            _logger.Info("Инициализация сервиса печати чеков Атол завершена");
        }

        public SessionOpeningStatus OpenSession(int cashierId, string cashierName)
        {

            _logger.Info($"Попытка открытия смены для кассира {cashierId} ({cashierName})");

            _logger.Info("Проверка статуса текущей смены");
            if (_printer.SessionOpened)
            {
                _logger.Info("Смена открыта");

                if (_printer.SessionExceedLimit)
                {
                    throw new InvalidOperationException("Текущая смена привысила 24 часа.");
                }

                _logger.Info("Получение информации по текущей смене");
                var session = _sessionStorage.Get();
                if (session == null)
                {
                    throw new InvalidOperationException("Информация по текущей смене не найдена");
                }

                _logger.Info($"Информация по текущей смене получена. Кассир {session.CashierId} ({session.CashierName}). Дата открытия: {session.SessionStart.ToString("dd.mm.yy hh:MM:ss")} ");

                if (session.CashierId == cashierId)
                {
                    throw new InvalidOperationException("Смена для запрошенного кассира уже открыта");
                }

                throw new InvalidOperationException("Смена открыта для другого кассира");
            }
            else
            {
                _logger.Info("Смена закрыта");

                var session = new CashierSession(cashierId, cashierName);

                _logger.Info("Переход в режим 1 для открытия смены");
                _printer.Mode = 1;
                _printer.Password = session.CashierId.ToString();
                if (_printer.SetMode() != 0)
                {
                    throw new InvalidOperationException($"Не удалось перейти в режим 1. \n{_printer.ResultCode} - { _printer.ResultDescription}");
                }

                SetCachier(session.CashierName);

                session.SessionStart = DateTime.Now;

                _sessionStorage.Set(session);

                _logger.Info("Открытие смены");
                if (_printer.OpenSession() != 0)
                {
                    _sessionStorage.Remove();
                    throw new InvalidOperationException($"Не удалось открыть сессию для кассира {session.CashierId} ({session.CashierName}).  \n{_printer.ResultCode} - { _printer.ResultDescription}");
                }
            }

            if (_printer.ResetMode() != 0)
            {
                throw new InvalidOperationException($"Не удалось перейти в режим выбора. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }

            return SessionOpeningStatus.Success;
        }

        public PrinterOperationStatus PrintReceipt(Receipt receipt)
        {
            _logger.Info("Печать чека начата");

            _logger.Info("Проверка статуса смены");
            if (!_printer.SessionOpened)
            {
                throw new InvalidOperationException("Смена закрыта.");
            }
            if (_printer.SessionExceedLimit)
            {
                throw new InvalidOperationException("Текущая смена привысила 24 часа.");
            }
            _logger.Info("Смена открыта");

            _logger.Info("Получение информации по текущей смене");
            var session = _sessionStorage.Get();
            if (session == null)
            {
                throw new InvalidOperationException("Информация по текущей смене не найдена");
            }
            _logger.Info($"Информация по текущей смене получена. Кассир {session.CashierId} ({session.CashierName}). Дата открытия: {session.SessionStart.ToString("dd.mm.yy hh:MM:ss")} ");

            SetCachier(session.CashierName);

            CloseCheck();

            _printer.Mode = 1;
            if (_printer.SetMode() != 0)
            {
                throw new InvalidOperationException($"Не удалось перейти в режим 1. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }

            _logger.Info("Создание чека");
            if (_printer.NewDocument() != 0)
            {
                throw new InvalidOperationException($"Не удалось создать чек. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }
            _logger.Info("Чек создан успешно");


            _logger.Info("Открытие чека");
            _printer.CheckType = 1;
            _printer.CheckMode = 1;
            if (_printer.OpenCheck() != 0)
            {
                throw new InvalidOperationException($"Не удалось открыть чек. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }
            _logger.Info("Чек открыт успешно");

            _logger.Info("Задание системы налогообложения: 2 (УСН доход)");
            _printer.AttrNumber = 1055;
            _printer.AttrValue = "2";
            if (_printer.WriteAttribute() != 0)
            {
                throw new InvalidOperationException($"Не удалось указать систему налогообложения. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }
            _logger.Info("Система налогообложения задана");


            _logger.Info("Печать позиций чека");
            foreach (var item in receipt.Items)
            {
                if (PrintItem(item) != 0)
                {
                    throw new InvalidOperationException($"Не удалось зарегистрировать позицию \"{item.Description}\" (цена: {item.Price}, количество: {item.Quantity}). \n{_printer.ResultCode} - { _printer.ResultDescription}");
                }
            }
            _logger.Info("Печать позиций чека завершена");

            _printer.TypeClose = receipt.IsPaydByCard ? 1 : 0;
            _printer.Summ = (double)receipt.Sum;
            _logger.Info($"Регистрация платежа на сумму {_printer.Summ}");
            if (_printer.Payment() != 0)
            {
                throw new InvalidOperationException($"Не удалось зарегистрировать платеж (сумма - {_printer.Summ}). \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }

            _logger.Info("Платеж зарегистрирован");

            _logger.Info("Закрытие чека");
            if (_printer.CloseCheck() != 0)
            {
                throw new InvalidOperationException($"Не удалось закрыть чек. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }
            _logger.Info("Чек закрыт");

            _logger.Info("Печать чека завершена успешно");

            if (_printer.ResetMode() != 0)
            {
                throw new InvalidOperationException($"Не удалось перейти в режим выбора. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }
            return PrinterOperationStatus.Success;
        }

        public PrinterOperationStatus PrintRefund(Receipt receipt)
        {
            _logger.Info("Печать чека начата");

            _logger.Info("Проверка статуса смены");
            if (!_printer.SessionOpened)
            {
                throw new InvalidOperationException("Не удалось напечатать чек. Смена закрыта.");
            }
            _logger.Info("Смена открыта");

            _logger.Info("Получение информации по текущей смене");
            var session = _sessionStorage.Get();
            if (session == null)
            {
                throw new InvalidOperationException("Информация по текущей смене не найдена");
            }
            _logger.Info($"Информация по текущей смене получена. Кассир {session.CashierId} ({session.CashierName}). Дата открытия: {session.SessionStart.ToString("dd.mm.yy hh:MM:ss")} ");

            SetCachier(session.CashierName);

            CloseCheck();

            _printer.Mode = 1;
            if (_printer.SetMode() != 0)
            {
                throw new InvalidOperationException($"Не удалось перейти в режим 1. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }

            _logger.Info("Создание чека");
            if (_printer.NewDocument() != 0)
            {
                throw new InvalidOperationException($"Не удалось создать чек. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }
            _logger.Info("Чек создан успешно");


            _logger.Info("Открытие чека");
            _printer.CheckType = 2;
            _printer.CheckMode = 1;
            if (_printer.OpenCheck() != 0)
            {
                throw new InvalidOperationException($"Не удалось открыть чек. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }
            _logger.Info("Чек открыт успешно");

            if (_printer.WriteAttribute() != 0)
            {
                throw new InvalidOperationException($"Не удалось указать систему налогообложения. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }
            _logger.Info("Система налогообложения задана");


            _logger.Info("Печать позиций чека");
            foreach (var item in receipt.Items)
            {
                if (PrintRefundItem(item) != 0)
                {
                    throw new InvalidOperationException($"Не удалось зарегистрировать позицию \"{item.Description}\" (цена: {item.Price}, количество: {item.Quantity}). \n{_printer.ResultCode} - { _printer.ResultDescription}");
                }
            }
            _logger.Info("Печать позиций чека завершена");

            _printer.TypeClose = receipt.IsPaydByCard ? 1 : 0;
            _printer.Summ = (double)receipt.Sum;
            _printer.Destination = 0;

            _logger.Info("Закрытие чека");
            if (_printer.CloseCheck() != 0)
            {
                throw new InvalidOperationException($"Не удалось закрыть чек. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }
            _logger.Info("Чек закрыт");

            _logger.Info("Печать чека завершена успешно");

            if (_printer.ResetMode() != 0)
            {
                throw new InvalidOperationException($"Не удалось перейти в режим выбора. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }
            return PrinterOperationStatus.Success;

        }

        public ZReportStatus PrintZReport()
        {
            _logger.Info("Снятие Z-отчета с закрытием смены");
            _logger.Info("Проверка статуса текущей смены");
            if (!_printer.SessionOpened)
            {
                _logger.Info("Текущая смена уже закрыта");
                _sessionStorage.Remove();
                return ZReportStatus.Success;
            }
            _logger.Info("Текущая смены открыта");

            _logger.Info("Получение информации по текущей смене");
            var session = _sessionStorage.Get();
            if (session == null)
            {
                throw new InvalidOperationException("Информация по текущей смене не найдена");
            }
            _logger.Info($"Информация по текущей смене получена. Кассир {session.CashierId} ({session.CashierName}). Дата открытия: {session.SessionStart.ToString("dd.mm.yy hh:MM:ss")} ");

            SetCachier(session.CashierName);

            _logger.Info("Переход в режим 3 для снятия z-отчета");
            _printer.Password = "30";
            _printer.Mode = 3;
            if (_printer.SetMode() != 0)
            {
                throw new InvalidOperationException($"Не удалось перейти в режим 3. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }

            _logger.Info("Печать z-отчета");
            _printer.ReportType = 1;
            if (_printer.Report() != 0)
            {
                throw new InvalidOperationException($"Не удалось сформировать отчет типа 3. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }
            _logger.Info("Печать z-отчета завершена успешно");

            _logger.Info("Закрытие текущей смены");
            _sessionStorage.Remove();
            _logger.Info("Текущая смена закрыта");

            if (_printer.ResetMode() != 0)
            {
                throw new InvalidOperationException($"Не удалось перейти в режим выбора. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }

            return ZReportStatus.Success;
        }

        public ZReportStatus PrintXReport()
        {
            _logger.Info("Снятие X-отчета с закрытием смены");
            _logger.Info("Проверка статуса текущей смены");
            if (!_printer.SessionOpened)
            {
                throw new InvalidOperationException("Текущая смены уже закрыта");
            }
            _logger.Info("Текущая смены открыта");

            _logger.Info("Получение информации по текущей смене");
            var session = _sessionStorage.Get();
            if (session == null)
            {
                throw new InvalidOperationException("Информация по текущей смене не найдена");
            }
            _logger.Info($"Информация по текущей смене получена. Кассир {session.CashierId} ({session.CashierName}). Дата открытия: {session.SessionStart.ToString("dd.mm.yy hh:MM:ss")} ");

            SetCachier(session.CashierName);

            _logger.Info("Переход в режим 3 для снятия z-отчета");
            // устанавливаем пароль администратора ККМ
            _printer.Password = "29";
            // входим в режим отчетов без гашения
            _printer.Mode = 2;
            if (_printer.SetMode() != 0)
            {
                throw new InvalidOperationException($"Не удалось перейти в режим 2. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }

            _logger.Info("Печать z-отчета");
            _printer.ReportType = 2;
            if (_printer.Report() != 0)
            {
                throw new InvalidOperationException($"Не удалось сформировать отчет типа 2. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }
            _logger.Info("Печать X-отчета завершена успешно");

            if (_printer.ResetMode() != 0)
            {
                throw new InvalidOperationException($"Не удалось перейти в режим выбора. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }

            return ZReportStatus.Success;
        }

        public int PrintItem(RecItem item)
        {
            _printer.Name = item.Description;
            _printer.Price = (double)item.UnitPrice;
            _printer.Quantity = item.Quantity;
            _printer.Department = 0;
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
            return _printer.Registration();
        }

        public int PrintRefundItem(RecItem item)
        {
            _printer.Name = item.Description;
            _printer.Price = (double)item.UnitPrice;
            _printer.Quantity = item.Quantity;
            // TaxTypeNumber - Номер налога:
            //     0 - Налог из секции
            //     1 - НДС 0%
            //     2 - НДС 10%
            //     3 - НДС 18%
            //     4 - НДС не облагается
            //     5 - НДС с расчётной ставкой 10%
            //     6 - НДС с расчётной ставкой 18%
            //_printer.TaxTypeNumber = 4;

            return _printer.Return();
        }

        /// <summary>
        /// Закрываем открытй чек
        /// </summary>
        private void CloseCheck()
        {
            if (_printer.CheckState != 0 && _printer.CancelCheck() != 0)
            {
                throw new InvalidOperationException($"Не удалось отменить печать чека. \n{_printer.ResultCode} - { _printer.ResultDescription}");
            }
        }

        private void CheckStatus()
        {
            if (_printer.ResultCode != 0)
            {
                throw new InvalidOperationException($"\n{_printer.ResultCode} - { _printer.ResultDescription}");
            }
        }

        public void BeginFiscalReceipt()
        {
            _printer.Mode = 1;
            _printer.SetMode();

            CheckStatus();

            _printer.NewDocument();
            CheckStatus();

            _printer.CheckType = 1;
            _printer.CheckMode = 1;

            _printer.OpenCheck();

            CheckStatus();
        }

        private void SetCachier(string name)
        {
            _logger.Info($"Задание имени кассира: {name}");

            Check.NotEmpty(name, nameof(name));
            _printer.AttrNumber = 1021;
            _printer.AttrValue = name;
            if (_printer.WriteAttribute() != 0)
            {
                throw new InvalidOperationException("Не удалось задать имя кассира");
            }
        }
        // Применяемая система налогооблажения в чеке:
        //     ОСН - 1
        //     УСН доход - 2
        //     УСН доход-расход - 4
        //     ЕНВД - 8
        //     ЕСН - 16
        //     ПСН - 32
        public void SetTaxing()
        {
            _printer.AttrNumber = 1055;
            _printer.AttrValue = "2";
            _printer.WriteAttribute();
            CheckStatus();
        }


        public void PrintPayment(double payment)
        {

            _printer.TypeClose = 0;
            _printer.Summ = (double)payment;
            _printer.Payment();

            CheckStatus();
        }

        public void CloseFiscalReceipt()
        {
            _printer.CloseCheck();
        }

        public void CancelCheck()
        {
            _printer.CancelCheck();
        }


        public void Dispose()
        {
            _printer.DeviceEnabled = false;
            GC.SuppressFinalize(this);
        }
    }
}
