using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Models
{
    public enum SessionOpeningStatus
    {
        /// <summary>
        /// Успешно
        /// </summary>
        Success = 0,
        /// <summary>
        /// Смена открыта данным кассиром
        /// </summary>
        Opened = 1,
        /// <summary>
        /// Сессия открыта другим кассиром
        /// </summary>
        OpenedByAnotherCashier = 2,
        /// <summary>
        /// Ошибка
        /// </summary>
        Error = 3,
        /// <summary>
        /// Превышена максимальная продолжительность сессии
        /// </summary>
        SessionLimitExeeded = 4,
        /// <summary>
        /// Не найдена информация о текущей сессии
        /// </summary>
        SessionNotFound = 5

    }
}
