using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Models
{
    public enum AtolStatusCode
    {
        /// <summary>
        /// Успешно
        /// </summary>
        Ok = 0,
        /// <summary>
        /// Нет связи
        /// </summary>
        NoConnection = -1,
        /// <summary>
        /// Порт отсутствует или занят другим приложением
        /// </summary>
        PortUnavailable = -3,
        /// <summary>
        /// Ключ защиты не найден
        /// </summary>
        SecurityKeyNotFound = -4,
        /// <summary>
        /// Работа прервана пользователем
        /// </summary>
        Aborted = -5,
        /// <summary>
        /// Недопустимое значение параметра
        /// </summary>
        BadParam = -6,
        /// <summary>
        /// Невозможно добавить устройство
        /// </summary>
        CannotAddDevice = -7,
        /// <summary>
        /// Невозможно удалить устройство
        /// </summary>
        CannotRemoveDevice = -8,
        /// <summary>
        /// Устройство не найдено
        /// </summary>
        DeviceNotFound = -9,
        /// <summary>
        /// Неверная последовательность команд
        /// </summary>
        WrongOrderOfComands = -10,
        /// <summary>
        /// Устройство не включено
        /// </summary>
        Off = -11,
        /// <summary>
        /// Команда не поддерживается данным устройством
        /// </summary>
        NotSupported = -12,
        /// <summary>
        /// Не удалось загрузить модуль
        /// </summary>
        CannotLoadModule = -13,
        /// <summary>
        /// Порт занят драйвером семейства Атол
        /// </summary>
        PortInUse = -14,
        /// <summary>
        /// Ошибка обмена с ККМ на нижнем уровне
        /// </summary>
        LowLevelError = -15,
        /// <summary>
        /// Не поддерживается в данном режиме
        /// </summary>
        WrongMode = -16,
        /// <summary>
        /// Нет элементов отчета
        /// </summary>
        NoElementsForReport = -17,
        /// <summary>
        /// Нет доступа к ключу реестра
        /// </summary>
        RegistryKeyAccessDenied = -19,
        /// <summary>
        /// Неизвестная ошибка или ошибка оборудования
        /// </summary>
        UnknownError =-199,
        /// <summary>
        /// Недостаточно денег при выплате
        /// </summary>
        NoMoney = -3800,
        /// <summary>
        /// Чек закрыт, необходимо открыть
        /// </summary>
        BillNeedToOpen = -3801,
        /// <summary>
        /// Чек открыт, необходимо закрытие
        /// </summary>
        BillNeedToClose = -3802,
        /// <summary>
        /// Переданная сумма регистрации превышает максимально возможное значение
        /// </summary>
        BadPrice = -3803,
        /// <summary>
        /// Передана неверное количествоо
        /// </summary>
        BadQuantity = -3804,
        /// <summary>
        /// Передана нулевая цена
        /// </summary>
        ZeroPrice = -3805,
        /// <summary>
        /// В ККМ нет бумаги
        /// </summary>
        NoPaper = -3807,
        /// <summary>
        /// ККМ находится в режиме ввода пароля
        /// </summary>
        InPasswordMode = -3808,
        /// <summary>
        /// Недопустимый ИНН
        /// </summary>
        BadINN = -3809,
        /// <summary>
        /// Сумма возврата или аннулирования больше накопленной суммы 
        /// </summary>
        BalanceLessThanRefundSum = -3810,
        /// <summary>
        /// Производится печать
        /// </summary>
        PrintingInProgress = -3811,
        /// <summary>
        /// Неверная величина скидки/надбавки
        /// </summary>
        BadDiscount = -3813,
        /// <summary>
        /// Операция после скидки/надбавки невозможна 
        /// </summary>
        NotSupportedWithDiscount = -3814,
        /// <summary>
        /// В ККМ передан неверный номер секции
        /// </summary>
        BadSection = -3815,
        /// <summary>
        /// передан неверный тип оплаты
        /// </summary>
        BadPaymentType = -3816,
        /// <summary>
        /// Переполнение при умножении
        /// </summary>
        MultiplicationOverflow = -3817,
        /// <summary>
        /// Операция запрещена в таблице настроек 
        /// </summary>
        OperationDenied = -3818,
        /// <summary>
        /// Переполнение итога чека 
        /// </summary>
        BillTotalOverflow = -3819,
        /// <summary>
        /// Переполнение контрольной ленты
        /// </summary>
        ControlTapeOverflow = -3820,
        /// <summary>
        /// Операция невозможна при открытом чеке возврата
        /// </summary>
        RefundBillNeedToClose = -3821,
        /// <summary>
        /// Смена превысила 24 часа
        /// </summary>
        ShiftTooLong = -3822,
        /// <summary>
        /// Скидка запрещена в таблице
        /// </summary>
        DiscountDenied = -3823,
        /// <summary>
        /// Аннулирование и возврат в одном чеке
        /// </summary>
        CancelWithRefund = -3824,
        /// <summary>
        /// Неверный пароль
        /// </summary>
        WrongPassword = -3825,
        /// <summary>
        /// Не переполнен буфер контрольной ленты
        /// </summary>
        ControlTapeNotOverflowed = -3826,
        /// <summary>
        /// Идет печать контрольной ленты 
        /// </summary>
        ControlTapePrintingInprogress = -3827,
        /// <summary>
        /// Смена закрыта
        /// </summary>
        ShifrClosed = -3828,
        /// <summary>
        /// Идет печать отчета
        /// </summary>
        ReportPrintingInProgress = -3829,
        /// <summary>
        /// Передана неверная дата
        /// </summary>
        BadDate = -3830,
        /// <summary>
        /// Передано неверное время
        /// </summary>
        BadTime = -3831,
    }
}
