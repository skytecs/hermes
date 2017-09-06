using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Models
{
    public enum PrinterOperationStatus
    {
        /// <summary>
        /// Завершено успешно
        /// </summary>
        Success= 0,
        /// <summary>
        /// Ошибка при печати
        /// </summary>
        PrintingError = 1,
        /// <summary>
        /// Смена закрыта
        /// </summary>
        SessionClosed = 2,
        /// <summary>
        /// Неверный кассир
        /// </summary>
        WrongCashier = 3,
        /// <summary>
        /// Информация по смене не найдена
        /// </summary>
        SessionNotFound = 4

    }
}
