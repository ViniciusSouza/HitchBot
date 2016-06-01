using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Utilities;
using Newtonsoft.Json;
using TinderLibrary;
using TinderModels.Facebook;
using TinderModels;
using System.Collections.Generic;
using tinderbot.luis;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;
using System.Timers;
using tinderbot.tablemodels;

namespace tinderbot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        #region [ Private Constants Fields ]

        private const int FeedTimerIntervalInMiliseconds = 20000;
        private const string MSG_ADD_BOT = "To start playing with HitchBot first you need to add the bot to the conversation";

        #endregion

        #region [ Private Fields ] 
        private CloudStorageAccount storageAccount = null;
        private CloudTable matchesTable = null;
        private CloudTable matchesHistoryTable = null;
        private Timer feedTimer = null;

        private WebApiApplication App
        {
            get
            {
                return (WebApiApplication)System.Web.HttpContext.Current.ApplicationInstance;
            }
        }

        private bool ReadyToTalk
        {
            get
            {
                try
                {
                    return bool.Parse(App.Session["ReadyToTalk"].ToString());
                }
                catch { return false; }
            }
            set { App.Session["ReadyToTalk"] = value; }
        }


        private bool TimerStarted
        {
            get
            {
                try
                {
                    return bool.Parse(App.Session["TimerStarted"].ToString());
                }
                catch { return false; }
            }
            set { App.Session["TimerStarted"] = value; }
        }

        TinderSession TinderSession { get; set; }

        string FacebookID
        {
            get
            {
                try { return App.Session["FacebookID"].ToString(); } catch { return string.Empty; }
            }
            set { App.Session["FacebookID"] = value; }
        }

        private string FacebookToken
        {
            get { try { return App.Session["FacebookToken"].ToString(); } catch { return string.Empty; } }
            set { App.Session["FacebookToken"] = value; }
        }

        private double Latitude
        {
            get
            {
                try
                {
                    return double.Parse(App.Session["Latitude"].ToString());
                }
                catch
                {
                    return 0;
                }
            }
            set { App.Session["Latitude"] = value; }
        }

        private double Longitude
        {
            get
            {
                try
                {
                    return double.Parse(App.Session["Longitude"].ToString());
                }
                catch
                {
                    return 0;
                }
            }
            set { App.Session["Longitude"] = value; }
        }


        private int BotSessionSetup
        {
            get
            {
                try
                {
                    return int.Parse(App.Session["BotSessionSetup"].ToString());
                }
                catch
                {
                    return 1;
                }
            }
            set { App.Session["BotSessionSetup"] = value; }
        }

        #endregion

        #region [  Constructor  ]
        public MessagesController()
        {
            InitializeStorageAccount();
            InitializeTableClient();
        }
        #endregion

        #region [ Initializers ] 
        private Timer GetFeedTimer(string SessionID)
        {
            if (App.Timers.Keys.Contains(SessionID))
            {
                this.feedTimer = App.Timers[SessionID];
            }
            else
            {
                this.feedTimer = new Timer(FeedTimerIntervalInMiliseconds);
                this.feedTimer.AutoReset = false;
                this.feedTimer.Enabled = false;
                this.feedTimer.Elapsed += this.FeedTimer_Elapsed;
                App.Timers.Add(SessionID, this.feedTimer);
            }

            return this.feedTimer;
        }

        private void InitializeStorageAccount()
        {
            this.storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["storageconnectionstring"]);
        }

        /// <summary>
        /// Create the table client
        /// </summary>
        private void InitializeTableClient()
        {
            CloudTableClient tableClient = this.storageAccount.CreateCloudTableClient();

            this.matchesTable = tableClient.GetTableReference("matchestable");
            this.matchesHistoryTable = tableClient.GetTableReference("matcheshistorytable");

            // Create the table if it doesn't exist.
            this.matchesTable.CreateIfNotExists();
            this.matchesHistoryTable.CreateIfNotExists();
        }
        #endregion


        #region [  Post Message  ]
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<Message> Post([FromBody]Message message)
        {
            if (message.Type == "Message")
            {
                var reply = message.CreateReplyMessage();

                if (message.BotUserData != null)
                {
                    if (ReadyToTalk)
                    {
                        var keys = App.TinderSessions.Keys.ToList<string>();
                        if (keys.IndexOf(message.BotUserData.ToString()) >= 0)
                        {
                            TinderSession = App.TinderSessions[message.BotUserData.ToString()];
                            if (TinderSession.FbSessionInfo == null)
                            {
                                BotSessionSetup = 1;
                                ReadyToTalk = false;
                            }
                            else
                            {
                                BotSessionSetup = 6;
                                switch (message.Text.ToLower())
                                {
                                    case "list": reply = ListMatches(message); break;
                                    case "start":
                                        this.feedTimer = GetFeedTimer(message.BotUserData.ToString());
                                        if (this.feedTimer != null)
                                        {
                                            this.feedTimer.Start();
                                            reply.Text = "Hitch bot has started to talk with your matches";
                                        }
                                        break;
                                    case "stop":
                                        this.feedTimer = GetFeedTimer(message.BotUserData.ToString());
                                        if (this.feedTimer != null)
                                        {
                                            this.feedTimer.Stop();
                                            reply.Text = "Hitch bot has stopped to talk with your matches";
                                        }
                                        break;
                                }
                            }
                        }
                        else
                        {
                            reply.Text = MSG_ADD_BOT;
                            BotSessionSetup = 0;
                            ReadyToTalk = false;
                        }
                    }
                    else
                    {

                        while (reply.Text == string.Empty && BotSessionSetup <= 4)
                        {
                            if (FacebookID == string.Empty)
                            {
                                if (BotSessionSetup == 0)
                                {
                                    BotSessionSetup = 1;
                                    reply.Text = "Add your Facebook ID";
                                }
                                else
                                {
                                    FacebookID = message.Text;
                                }

                            }
                            else if (FacebookToken == string.Empty)
                            {
                                if (BotSessionSetup == 1)
                                {
                                    BotSessionSetup = 2;
                                    reply.Text = "Add your Facebook Token";
                                }
                                else
                                {
                                    FacebookToken = message.Text;
                                }
                            }
                            else if (Latitude == 0)
                            {
                                if (BotSessionSetup == 2)
                                {
                                    BotSessionSetup = 3;
                                    reply.Text = "Add your Latitude";
                                }
                                else
                                {
                                    try
                                    {
                                        Latitude = double.Parse(message.Text);
                                        if (Latitude < -85 || Latitude > 85)
                                            reply.Text = "The latitude is out of range, please try enter it again number between -85 and 85";
                                    }
                                    catch
                                    {
                                        reply.Text = "An Error has occurred during the latitude parsing, please try enter it again";
                                    }
                                }
                            }
                            else
                            {
                                if (BotSessionSetup == 3)
                                {
                                    BotSessionSetup = 4;
                                    reply.Text = "Add your Longitude";
                                }
                                else
                                {
                                    try
                                    {
                                        Longitude = double.Parse(message.Text);
                                        if (Longitude < -180 || Longitude > 180)
                                            reply.Text = "The longitude is out of range, please try enter it again number between -180 and 180";
                                        else
                                            BotSessionSetup = 5;
                                    }
                                    catch
                                    {
                                        reply.Text = "An Error has occurred during the longitude parsing, please try enter it again";
                                    }
                                }
                            }
                        }

                        if (BotSessionSetup == 5)
                        {
                            var fbsession = new FacebookSessionInfo();
                            fbsession.FacebookID = FacebookID;
                            fbsession.FacebookToken = FacebookToken;

                            Position location = new Position();
                            location.Latitude = Latitude;
                            location.Longitude = Longitude;

                            this.TinderSession = TinderSession.CreateNewSession(fbsession, location);

                            try
                            {
                                //This method besides the authentication, sets the location, calls the update and checks for recommendations
                                await this.TinderSession.Authenticate();
                                BotSessionSetup = 6;
                                ReadyToTalk = true;
                                reply.Text = $"Your Tinder account is connected to HitchBot! \r\n You have {this.TinderSession.Matches.Count} Matches";

                                //Update Match status and upload the data do AzureTable
                                await ProcessMatches();

                            }
                            catch (Exception ex)
                            {
                                reply.Text = $"Some error has occurred: { ex.Message }. Please try adding your facebook and location again";
                                BotSessionSetup = 1;
                                FacebookID = string.Empty;
                                FacebookToken = string.Empty;
                                Longitude = 0;
                                Latitude = 0;
                                ReadyToTalk = false;
                            }


                            try
                            {
                                App.TinderSessions.Add(message.BotUserData.ToString(), this.TinderSession);
                            }
                            catch (ArgumentException)
                            {
                                //There is an element using the same key, so just update the object and you are okay to go
                                App.TinderSessions[message.BotUserData.ToString()] = this.TinderSession;
                            }
                        }
                    }

                }
                else
                {
                    reply.Text = MessagesController.MSG_ADD_BOT;
                    BotSessionSetup = 0;
                }


                return reply;
            }
            else
            {
                return HandleSystemMessage(message);
            }
        }
        #endregion


        #region [  ProcessMatches  ]
        private async Task ProcessMatches()
        {
            if (this.TinderSession == null)
                return;

            var insertBatchOperation = new TableBatchOperation();

            //get all the matches and upload it to matches table
            foreach (Match match in this.TinderSession.Matches)
            {

                // Create a retrieve operation that takes a customer entity.
                TableOperation retrieveOperation = TableOperation.Retrieve<Matches>(this.TinderSession.CurrentUser.Id, match.Id);

                // Execute the retrieve operation.
                TableResult retrievedResult = matchesTable.Execute(retrieveOperation);

                // Print the phone number of the result.
                if (retrievedResult.Result != null)
                {
                    var retrievedMatch = (Matches)retrievedResult.Result;
                    if (retrievedMatch.DateOpportunity)
                    {
                        //Check if the state will be saved in memory or if I need to delete it from the collection
                        match.DateOpportunity = true;
                    }
                }
                else
                {

                    var matchRecord = new Matches();
                    matchRecord.PartitionKey = this.TinderSession.CurrentUser.Id;
                    matchRecord.RowKey = match.Id;
                    matchRecord.Timestamp = DateTimeOffset.Now;
                    matchRecord.DateOpportunity = false;

                    insertBatchOperation.Insert(matchRecord);

                }

            }

            //Insert all the itens that need to be included
            if (insertBatchOperation.Count > 0)
                await matchesTable.ExecuteBatchAsync(insertBatchOperation);

        }
        #endregion


        #region [ Feed Timer Elapsed  ]
        /// <summary>
        /// This method will be executed from time to time to update the tinder session information and actually talk with the user
        /// Using the Luiz Integration 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FeedTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Update the Session due the amount of time need to wait
            TinderSesisonUpdate();

            foreach (Match match in TinderSession.Matches)
            {
                if (!match.DateOpportunity)
                {
                    List<string> Messages = new List<string>();
                    var mensagem = string.Empty;

                    //First talk, if there is no message send "Hi"
                    if (match.Messages.Count() == 0)
                    {
                        mensagem = "Hi";
                    }
                    else
                    {
                        // check if the last message was sent from the current user
                        Msg lastMessage = match.Messages.Last<Msg>();

                        if (!lastMessage.From.Equals(this.TinderSession.CurrentUser.Id))
                        {
                            //Call Luis
                            var luisResponse = CallLuisApi(lastMessage.Message);

                            foreach (Intents intents in luisResponse.Intents)
                            {
                                if (intents.Score > 0.7)
                                {
                                    switch (intents.Intent.ToLower())
                                    {
                                        case "sayhello": mensagem = "Hello, How are you?"; break;
                                        case "sayage": mensagem = "I'm 25 year old, but I don't look my age :)"; break;
                                        case "datingopportunity":
                                            mensagem = "I'll think about it!  ;)";
                                            match.DateOpportunity = true; //objective accomplished
                                            break;
                                        case "sayhowisshe": mensagem = "I'm fine, thanks for asking"; break;
                                        case "askwhatdoyoudo": mensagem = "I work for a digital agency"; break;
                                        case "sayjob": mensagem = "I'm a digital marketing"; break;
                                        case "askhowareyou": mensagem = "Not too bad"; break;
                                    }
                                }

                                if (!string.IsNullOrEmpty(mensagem))
                                {
                                    ProcessMessage(match, mensagem, intents);

                                    //If there is one intent satisfied didin't check the others
                                    break;
                                }
                            }
                        }
                    }

                }

            }

            this.feedTimer.Start();
        }
        #endregion

        #region [  Tinder Session Update  ]
        private void TinderSesisonUpdate()
        {
            var tinderUpdateTask = this.TinderSession.GetUpdate();
            tinderUpdateTask.ConfigureAwait(true);
        }
        #endregion


        #region [  CallLuisApi  ]
        private LuisResponse CallLuisApi(string message)
        {
            LuisResponse luisResponse = null;
            try
            {
                var luis = new HttpClient();
                var luisTask = luis.GetAsync("https://api.projectoxford.ai/luis/v1/application?id=a444ceab-0ef2-4582-bc04-f869bc30dc84&subscription-key=c86fa102ab1947b79e8d615452fcfa31&q=" + message);
                luisTask.ConfigureAwait(true);

                var response = luisTask.Result;

                var responseDataTask = response.Content.ReadAsStringAsync();
                responseDataTask.ConfigureAwait(true);

                luisResponse = JsonConvert.DeserializeObject<LuisResponse>(responseDataTask.Result);
            }
            catch { };
            return luisResponse;

        }
        #endregion

        #region [  Process Message  ]
        private void ProcessMessage(Match match, string mensagem, Intents intent)
        {
            if (match.DateOpportunity)
            {
                //Update the azure table register
                // Create a retrieve operation that takes a customer entity.
                TableOperation retrieveOperation = TableOperation.Retrieve<Matches>(this.TinderSession.CurrentUser.Id, match.Id);

                // Execute the retrieve operation.
                TableResult retrievedResult = matchesTable.Execute(retrieveOperation);

                // Print the phone number of the result.
                if (retrievedResult.Result != null)
                {
                    var retrievedMatch = (Matches)retrievedResult.Result;
                    retrievedMatch.DateOpportunity = true;

                    TableOperation updateOperation = TableOperation.Replace(retrievedMatch);

                    // Execute the operation.
                    matchesTable.Execute(updateOperation);

                }

                //TODO: Call the user, send some sort of notification
            }


            //Send the message to the user
            var tinderMessageTask = this.TinderSession.SendMessage(match.Id, mensagem);
            tinderMessageTask.ConfigureAwait(true);

            //Register the message on matchesHistory table
            var matchHistory = new MatchesHistory();
            matchHistory.PartitionKey = this.TinderSession.CurrentUser.Id;
            matchHistory.RowKey = match.Id;
            matchHistory.Timestamp = DateTimeOffset.Now;
            matchHistory.Message = mensagem;
            matchHistory.Intent = intent.Intent;
            matchHistory.Score = intent.Score;

            TableOperation insertOperation = TableOperation.Insert(matchHistory);
            var insertTask = matchesHistoryTable.ExecuteAsync(insertOperation);
            insertTask.ConfigureAwait(false);

        }
        #endregion

        #region [  ListMaches  ]
        private Message ListMatches(Message message)
        {
            var reply = message.CreateReplyMessage();

            foreach (Match match in this.TinderSession.Matches)
            {
                reply.Text += $"{match.Person.Name }|";
            }
            return reply;
        }
        #endregion


        #region [ Bot HandleSystemMessage  ]
        private Message HandleSystemMessage(Message message)
        {
            if (message.Type == "Ping")
            {
                Message reply = message.CreateReplyMessage("Pong");
                reply.Type = "Ping";
                return reply;
            }
            else if (message.Type == "DeleteUserData")
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == "BotAddedToConversation")
            {
                //Import call this code first to add the session to TinderSessions 
                if (message.BotUserData == null)
                {
                    message.BotUserData = Guid.NewGuid().ToString();
                }

                Message reply = message.CreateReplyMessage("Hello I'm HitchBot, and I'll help you out, but first to connect with the Tinder service I'll need some information on that specific order:\r\n 1) Your facebook ID; \r\n2) Your Facebook Token; \r\n3) Latitude; \r\n4) Longitude");
                reply.BotUserData = message.BotUserData;

                FacebookID = string.Empty;
                FacebookToken = string.Empty;

                Longitude = 0;
                Latitude = 0;

                BotSessionSetup = 1;
                ReadyToTalk = false;

                this.TinderSession = new TinderSession();

                try
                {
                    App.TinderSessions.Add(message.BotUserData.ToString(), this.TinderSession);
                }
                catch (ArgumentException)
                {
                    //There is an element using the same key, so just update the object and you are okay to go
                    App.TinderSessions[message.BotUserData.ToString()] = this.TinderSession;
                }

                reply.Type = "BotAddedToConversation";
                return reply;
            }
            else if (message.Type == "BotRemovedFromConversation")
            {
            }
            else if (message.Type == "UserAddedToConversation")
            {
            }
            else if (message.Type == "UserRemovedFromConversation")
            {
            }
            else if (message.Type == "EndOfConversation")
            {
            }

            return null;
        }
        #endregion
    }
}