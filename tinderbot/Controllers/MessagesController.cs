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

namespace tinderbot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        const string MSG_ADD_BOT = "To start playing with HitchBot first you need to add the bot to the conversation";

        WebApiApplication App
        {
            get
            {
                return (WebApiApplication)System.Web.HttpContext.Current.ApplicationInstance;
            }
        }

        bool ReadyToTalk
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

        TinderSession TinderSession { get; set; }

        string FacebookID
        {
            get
            {
                try { return App.Session["FacebookID"].ToString(); } catch { return string.Empty; }
            }
            set { App.Session["FacebookID"] = value; }
        }

        string FacebookToken
        {
            get { try { return App.Session["FacebookToken"].ToString(); } catch { return string.Empty; } }
            set { App.Session["FacebookToken"] = value; }
        }

        double Latitude
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

        double Longitude
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


        int BotSessionSetup
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
                                    case "hitch": await timer(); break;
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

                //reply.Text = message.BotUserData.ToString();

                //if (App.TinderSessions)

                //// calculate something for us to return
                //int length = (message.Text ?? string.Empty).Length;

                //// return our reply to the user
                //return message.CreateReplyMessage($"You sent {length} characters");
                return reply;
            }
            else
            {
                return HandleSystemMessage(message);
            }
        }

        private async Task timer()
        {
            await TinderSession.GetUpdate();

            foreach (Match match in TinderSession.Matches)
            {
                List<string> Messages = new List<string>();
                var mensagem = string.Empty;
                var added = false;

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
                        var luis = new HttpClient();
                        var response = await luis.GetAsync("https://api.projectoxford.ai/luis/v1/application?id=a444ceab-0ef2-4582-bc04-f869bc30dc84&subscription-key=c86fa102ab1947b79e8d615452fcfa31&q=" + lastMessage.Message);
                        var responseData = await response.Content.ReadAsStringAsync();

                        var LuisResponse = JsonConvert.DeserializeObject<LuisResponse>(responseData);

                        foreach(Intents intents in LuisResponse.Intents)
                        {
                            if(intents.Score > 0.7)
                            {
                                switch (intents.Intent.ToLower())
                                {
                                    case "sayhello": mensagem = "Hello, How are you?"; break;
                                    case "sayage": mensagem = "I'm 25 year old, but I don't look my age :)"; break;
                                    case "datingopportunity": mensagem = ""; break;
                                    case "sayhowisshe": mensagem = "I'm fine, thanks for asking"; break;
                                    case "askwhatdoyoudo": mensagem = "I work for a digital agency"; break;
                                    case "sayjob": mensagem = "I'm a digital marketing"; break;
                                    case "askhowareyou": mensagem = "Not too bad"; break;
                                }
                            }
                        }
                    }
                }

                if (!mensagem.Equals(string.Empty))
                {
                    await TinderSession.SendMessage(match.Id, mensagem);
                    Messages.Add(mensagem);
                    try
                    {
                        App.Conversations.Add(match.Id, Messages);
                    }
                    catch {
                        App.Conversations[match.Id] = Messages;
                    }
                }
                
            }
        }

        private Message ListMatches(Message message)
        {
            var reply = message.CreateReplyMessage();

            foreach (Match match in this.TinderSession.Matches)
            {
                reply.Text += $"{match.Person.Name }|";
            }
            return reply;
        }


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
    }
}