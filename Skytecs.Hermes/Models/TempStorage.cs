using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Skytecs.Hermes.Models
{
    public class TempStorage : ISessionStorage
    {
        private CashierSession _session;

        private readonly string _path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Session.xml");

        public CashierSession Set(CashierSession session)
        {
            using (var file = System.IO.File.Create(_path))
            {
                new XmlSerializer(session.GetType()).Serialize(file, session);
            }

            _session = session;

            return _session;

        }

        public CashierSession Get()
        {
            if (_session == null && File.Exists(_path))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(CashierSession));
                using (StreamReader reader = new StreamReader(_path))
                {
                    _session = (CashierSession)serializer.Deserialize(reader);
                }
            }

            return _session;
        }

        public void Remove()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            _session = null;
        }

    }
}
