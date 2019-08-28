using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeleSharp.TL;
using TeleSharp.TL.Contacts;
using TeleSharp.TL.Help;
using TLSharp.Core;

namespace TgPhoneChecker.Logic
{
    internal enum Status
    {
        Running,
        Pausing,
        Stopped,
        Done,
    }

    internal class PhoneExistChecker
    {
        private TelegramClient Client { get; set; }
        private List<string> PhoneList { get; set; }

        public Status Status { get; set; }
        public Dictionary<string, bool?> ResultList { get; set; }

        public event EventHandler<KeyValuePair<string, bool?>> OnePhoneChecked;

        public PhoneExistChecker(TelegramClient client, IEnumerable<string> phoneList)
        {
            Client = client;
            PhoneList = (phoneList as List<string>) ?? phoneList.ToList();
            Status = Status.Stopped;
            ResultList = new Dictionary<string, bool?>();
        }

        private async Task<Tuple<string, bool?>> Check(int i = 0)
        {
            while (PhoneList.Count == 0)
                await Task.Run(() => Thread.Sleep(1));
            var phone = PhoneList.First();

            try
            {
                TLRequestImportContacts requestImportContacts = new TLRequestImportContacts();
                requestImportContacts.Contacts = new TLVector<TLInputPhoneContact>();
                requestImportContacts.Contacts.Add(new TLInputPhoneContact()
                {
                    Phone = phone,
                    FirstName = "",
                    LastName = ""
                });
                var o2 = await Client.SendRequestAsync<TLImportedContacts>(requestImportContacts);

                return new Tuple<string, bool?>(phone, o2.Imported.Count > 0);
            }
            catch (TLSharp.Core.Network.FloodException err)
            {
                await Task.Run(() => Thread.Sleep(err.TimeToWait));
                return await Check();
            }
            catch when (i < 5)
            {
                return await Check(i + 1);
            }
            catch
            {
                return null;
            }
        }

        private async void StartCheck()
        {
            Status = Status.Running;
            while (Status == Status.Running)
            {
                var result = await Check();
                ResultList.Add(result.Item1, result.Item2);
                OnePhoneChecked?.Invoke(this, ResultList.Last());
                PhoneList.RemoveAt(0);
            }
            if (Status == Status.Pausing)
                Status = Status.Stopped;
        }

        public void Start()
        {
            switch (Status)
            {
                case Status.Pausing:
                    while (Status == Status.Pausing)
                        Thread.Sleep(1);
                    Start();
                    break;
                case Status.Stopped:
                    StartCheck();
                    break;
                case Status.Running:
                default:
                    break;
            }
        }
        public void Pause()
        {
            switch (Status)
            {
                case Status.Running:
                    Status = Status.Pausing;
                    while (Status != Status.Stopped)
                        Thread.Sleep(1);
                    break;
                case Status.Stopped:
                case Status.Done:
                case Status.Pausing:
                default:
                    break;
            }
        }
    }
}
