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

namespace tinderbot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {


        WebApiApplication App
        {
            get
            {
                return (WebApiApplication)System.Web.HttpContext.Current.ApplicationInstance;
            }
        }

        TinderSession tinder;


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
                    var keys = App.TinderSessions.Keys.ToList<string>();
                    if (keys.IndexOf(message.BotUserData.ToString()) >= 0)
                    {
                        var tinder = App.TinderSessions[message.BotUserData.ToString()];
                        if (tinder.FbSessionInfo == null)
                        {
                            reply.Text = "Add the bot to the convertion to setup the object";

                            //reply.Text = "Before I start playing I need more information, type fb_id:<facebook id> and fb_token:<facebook token>";

                            //var fb_id = string.Empty;
                            //var fb_token = string.Empty;

                            ///TODO: Parse the answer
                            //var idx = message.Text.IndexOf("fb_id:");
                            //var length = message.Text.IndexOf(" ", idx + "fb_id:".Length + 1) - idx;
                            //fb_id = message.Text.Substring(idx, length);

                        }
                        else {

                            //This method besides the authentication, sets the location, calls the update and checks for recommendations
                            await tinder.Authenticate();
                        }
                    }
                    else
                    {
                        reply.Text = "Add this bot to the conversation to start over";
                    }
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

                Message reply = message.CreateReplyMessage("Hello I'm HitchBot, and I'll help you out, but first to connect with the Tinder service I'll need two informations your facebook ID and Your Facebook Token.");
                reply.BotUserData = message.BotUserData;

                var fbsession = new FacebookSessionInfo();
                fbsession.FacebookID = "100012178426193";
                fbsession.FacebookToken = "EAAGm0PX4ZCpsBAIKNgJoN3TCSmaKBukjodZCNwbdmQxJt2cBPoSgUkOZBYhRQE2sRe4Y5VkRjUjLvnFTFR2nEvJYdLTP0HTYAzh7DCMkQWeSQZCG0ItQmZAymuxY3TZAdYFBZBVZBnzyb3ppNuZBr7vaaPMzhMk9KUVFIy7B2F0W5bmAZCV49i4CTI";

                Position location = new Position();
                location.Latitude = 47.620499;
                location.Longitude = -122.350876;

                var session = TinderSession.CreateNewSession(fbsession, location);

                try
                {
                    App.TinderSessions.Add(message.BotUserData.ToString(), session);
                }
                catch (ArgumentException ex) {
                    App.TinderSessions[message.BotUserData.ToString()] = session;
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