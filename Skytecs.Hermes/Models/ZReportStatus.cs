using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Models
{
    public enum ZReportStatus
    {
        /// <summary>
        /// Успешно
        /// </summary>
        Success = 0,
        /// <summary>
        /// Сессия закрыта
        /// </summary>
        SessionClosed = 1,
        /// <summary>
        /// ошибка
        /// </summary>
        Error = 2,
        /// <summary>
        /// Смена не найдена в хранилище
        /// </summary>
        SessionNotFound = 3
    }
}
