using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Models
{
    public interface ISessionStorage
    {
        /// <summary>
        /// Получить текущую сессию (открытую)
        /// </summary>
        /// <returns></returns>
        CashierSession Get();
        /// <summary>
        /// Сохранить открытую сессию
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        CashierSession Set(CashierSession session);
        /// <summary>
        /// Удалить сессию
        /// </summary>
        void Remove();
    }
}
