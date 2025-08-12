using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamsFileNotifier.Messaging;
using TeamsFileNotifier.Global;

namespace TeamsFileNotifier.Parsing
{
    internal class Crestron
    {
        private MessageBroker _messaging;

        public Crestron(MessageBroker messaging)
        {
            _messaging = messaging;
            _messaging.Subscribe<FileChangedMessage>(this.OnCrestronFileChanged);
        }

        private void OnCrestronFileChanged(FileChangedMessage fileChangedMessage)
        {

        }
    }
}
