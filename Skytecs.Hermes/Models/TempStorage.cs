﻿using System;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Xml.Serialization;
using Skytecs.Hermes.Utilities;

namespace Skytecs.Hermes.Models
{
    public class TempStorage : ISessionStorage
    {
        private static readonly XmlSerializer SessionSerializer = new XmlSerializer(typeof(CashierSession));

        private CashierSession _session;

        private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Skytecs", "Hermes", "Session.xml");

        static TempStorage()
        {
            var rules = new DirectorySecurity();

            var localUsers = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

            rules.AddAccessRule(new FileSystemAccessRule(localUsers, FileSystemRights.FullControl, AccessControlType.Allow));

            Directory.CreateDirectory(Path.GetDirectoryName(FilePath), rules);
        }

        public CashierSession Set(CashierSession session)
        {
            Check.NotNull(session, nameof(session));

            using (var file = File.Create(FilePath))
            {
                SessionSerializer.Serialize(file, session);
            }

            _session = session;

            return _session;

        }

        public CashierSession Get()
        {
            if (_session == null && File.Exists(FilePath))
            {
                using (StreamReader reader = new StreamReader(FilePath))
                {
                    _session = (CashierSession)SessionSerializer.Deserialize(reader);
                }
            }

            return _session;
        }

        public void Remove()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }

            _session = null;
        }

    }
}
