using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using BotCampDemo.Model;
using Microsoft.Bot.Connector;
using Microsoft.Cognitive.LUIS;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Vision;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BotCampDemo
{
	[BotAuthentication]
	public class MessagesController : ApiController
	{
		/// <summary>
		/// POST: api/Messages
		/// Receive a message from a user and reply to it
		/// </summary>
		public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
		{
			if (activity.Type == ActivityTypes.Message)
			{
				Trace.TraceInformation(JsonConvert.SerializeObject(activity, Formatting.Indented));
				ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
				Activity reply = activity.CreateReply();
				//reply.Text = "看不懂";

				if (activity.Attachments?.Count > 0 && activity.Attachments.First().ContentType.StartsWith("image"))
				{
					ImageTemplate(reply, activity.Attachments.First().ContentUrl);
				}
				else
				{
					var fbData = JsonConvert.DeserializeObject<FBChannelModel>(activity.ChannelData.ToString());
					if (fbData.postback != null)
					{
						var url = fbData.postback.payload.Split('>')[1];
						if (fbData.postback.payload.StartsWith("Face>"))
						{
							//faceAPI
							FaceServiceClient client = new FaceServiceClient("01938386d18549e6bc1575b036d6d169");
							var result = await client.DetectAsync(url, true, false, new FaceAttributeType[] { FaceAttributeType.Age, FaceAttributeType.Gender });
							//reply.Text = $"male:{result.Count(x => x.FaceAttributes.Gender == "male")},female:{result.Count(x => x.FaceAttributes.Gender == "female")}";

							foreach (var face in result)
							{
								reply.Text += face.FaceAttributes.Gender + "（" + face.FaceAttributes.Age + "） ";
							}
						}
						else if (fbData.postback.payload.StartsWith("Analyze>"))
						{
							//辨識圖片
							VisionServiceClient client = new VisionServiceClient("5092ac9edbb1474ebc4c209d551036ce");
							var result = await client.AnalyzeImageAsync(url, new VisualFeature[] { VisualFeature.Description });
							reply.Text = result.Description.Captions.First().Text;
						}
					}
					else
					{
						using (LuisClient client = new LuisClient("1a5eff99-4dbd-4b86-8c2c-2c7b314493ca", "f9a1366042a3474eaa9c4c3ddd882dd2"))
						{
							var result = await client.Predict(activity.Text);
							if (result.Intents.Count() > 0)
							{
                                if (result.TopScoringIntent.Name == "找車") 
                                {
                                    if(result.Entities.Count() > 0 ) 
                                    {
                                        var car = "";
                                        var address = "";

                                        var entities = result.GetAllEntities();
                                        foreach (Microsoft.Cognitive.LUIS.Entity entity in entities)
										{
                                            if( entity.Name == "車型::計程車") 
                                            {
                                                car = entity.Value;
                                            }
                                            else if (entity.Name == "地點")
                                            {
                                                address = entity.Value.Replace(" ", "");

												var webClient = new WebClient();
												client.Encoding = Encoding.UTF8;
												var url = "http://52.197.124.196/luis/index.php?action=getGoogleAddress&address=" + address;
												string result2 = webClient.DownloadString(url);
                                                address = System.Net.WebUtility.HtmlDecode(result2);
                                            }
										}

										//reply.Text = "請問你是否要在\"" + address + "\"上車？";


										reply.ChannelData = JObject.FromObject(new
										{
											attachment = new
											{
												type = "template",
												payload = new
												{
													template_type = "button",
                                                    text = "請問你是否要在『" + address + "』上車？",
													buttons = new List<object>()
													{
														new
														{
															type = "web_url",
															url = "https://17-vr-live.wonliao.com/luis/?car_type=1&address=" + address,
															title = "Yes",
															webview_height_ratio = "compact"
														},
														new
														{
															type = "postback",
															title = "No",
			                                                payload = "USER_DEFINED_PAYLOAD"
														}
													}
												}
											}
										});

									}





                                    //var _str = result.TopScoringIntent.Name;
                                    //reply.Text = $"won test 1 ==> {_str}";
                                    //TemplateByWebURL(reply);

                                } else {
                                 
                                    reply.Text = "看不懂";
                                }


                                /*
								if (result.TopScoringIntent.Name == "查匯率")
								{
									var currency = result.Entities?.Where(x => x.Key.StartsWith("幣別"))?.First().Value[0].Value;
									// ask api
									reply.Text = $"{currency}價格是30.0";
								}
								else if (result.TopScoringIntent.Name == "找車")
								{
									reply.Text = "請問你的上車地點?";
                                }
								else if (result.TopScoringIntent.Name == "找地點")
								{
                                    var address = result.Entities.First().Value[0].Value;
									reply.Text = $"你的上車地點是{address}";
								}
                                else 
                                {
                                    //TemplateByChannelData(reply);
                                    //TemplateByAirlineCheckin(reply);
                                    TemplateByWebURL(reply);
								}
								*/
							}
							else
							{
								reply.Text = "看不懂";
								
							}
						}
					}
				}


				await connector.Conversations.ReplyToActivityAsync(reply);
			}
			else
			{
				HandleSystemMessage(activity);
			}
			var response = Request.CreateResponse(HttpStatusCode.OK);
			return response;
		}

		private void TemplateByChannelData(Activity reply)
		{
			reply.ChannelData = JObject.FromObject(new
			{
				attachment = new
				{
					type = "template",
					payload = new
					{
						template_type = "generic",
						elements = new List<object>()
						{
							new
							{
								title = "iPad Pro",
								image_url = "https://s.yimg.com/wb/images/936392DB6B69D9C6D1B897B8DAB20AE595E96FA4",
								buttons = new List<object>()
								{
									new
									{
										type = "web_url",
										title = "YAHOO購物中心url",
										url = "https://tw.buy.yahoo.com/gdsale/MM172-6798747.html",
										webview_height_ratio = "tall"
									}
								}
							},
							new
							{
								title = "Surface Pro",
								image_url = "https://s.yimg.com/wb/images/268917ABD27238C9A20428002A8143AEEF40A048",
								buttons = new List<object>()
								{
									new
									{
										type = "web_url",
										title = "YAHOO購物中心url",
										url = "https://tw.buy.yahoo.com/gdsale/gdsale.asp?act=gdsearch&gdid=6561885",
										webview_height_ratio = "tall"
									}
								}
							}
						}
					}
				}
			});
		}

		private void TemplateByAirlineCheckin(Activity reply)
		{
			reply.ChannelData = JObject.FromObject(new
			{
				attachment = new
				{
					type = "template",
					payload = new
					{
						template_type = "airline_checkin",
						intro_message = "Check-in is available now.",
                        locale = "zh_TW",
                        theme_color = "#FFC500",
						pnr_number = "55688 台灣大車隊",
						flight_info = new List<object>()
						{
                            new
                            {
								flight_number = "AA-1234",
								departure_airport = new
								{
									airport_code = "家裡",
									city = "三重市永福街",
									terminal = "T4",
									gate = "G8"
								},
								arrival_airport = new
								{
									airport_code = "公司",
									city = "台北市長安東路",
									terminal = "T4",
									gate = "G8"
								},
								flight_schedule = new
								{
									boarding_time = "2016-01-05T15:05",
									departure_time = "2016-01-05T15:45",
									arrival_time = "2016-01-05T17:30"
								}
                            }
						},
                        checkin_url = "https://www.airline.com/check-in"
					}
				}
			});
		}

		private void TemplateByWebURL(Activity reply)
		{
			reply.ChannelData = JObject.FromObject(new
			{
				attachment = new
				{
					type = "template",
					payload = new
					{
						template_type = "button",
						text = "55688訂車",
						buttons = new List<object>()
						{
							new
							{
								type = "web_url",
								url = "https://17-vr-live.wonliao.com/luis/?car_type=1&address=%E6%96%B0%E5%8C%97%E5%B8%82%E4%B8%89%E9%87%8D%E5%8D%80%E6%B0%B8%E7%A6%8F%E8%A1%97135%E5%B7%B725%E8%99%9F",
								title = "打開網頁",
								webview_height_ratio = "compact"
							}
						}
					}
				}
			});
		}

		private void TemplateBySDK(Activity reply)
		{
			List<Attachment> att = new List<Attachment>();
			att.Add(new HeroCard()
			{
				Title = "iPad Pro",
				Images = new List<CardImage>() { new CardImage("https://s.yimg.com/wb/images/936392DB6B69D9C6D1B897B8DAB20AE595E96FA4") },
				Buttons = new List<CardAction>()
						{
							new CardAction(ActionTypes.OpenUrl, "Yahoo購物中心", value: $"https://tw.buy.yahoo.com/gdsale/MM172-6798747.html")
						}
			}.ToAttachment());
			att.Add(new HeroCard()
			{
				Title = "Surface Pro",
				Images = new List<CardImage>() { new CardImage("https://s.yimg.com/wb/images/268917ABD27238C9A20428002A8143AEEF40A048") },
				Buttons = new List<CardAction>()
						{
							new CardAction(ActionTypes.OpenUrl, "Yahoo購物中心", value: $"https://tw.buy.yahoo.com/gdsale/gdsale.asp?act=gdsearch&gdid=6561885")
						}
			}.ToAttachment());
			reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
			reply.Attachments = att;
		}

		private void ImageTemplate(Activity reply, string url)
		{
			List<Attachment> att = new List<Attachment>();
			att.Add(new HeroCard()
			{
				Title = "Cognitive services",
				Subtitle = "Select from below",
				Images = new List<CardImage>() { new CardImage(url) },
				Buttons = new List<CardAction>()
				{
					new CardAction(ActionTypes.PostBack, "男女生", value: $"Face>{url}"),
					new CardAction(ActionTypes.PostBack, "辨識圖片", value: $"Analyze>{url}")
				}
			}.ToAttachment());

			reply.Attachments = att;
		}

		private Activity HandleSystemMessage(Activity message)
		{
			if (message.Type == ActivityTypes.DeleteUserData)
			{
				// Implement user deletion here
				// If we handle user deletion, return a real message
			}
			else if (message.Type == ActivityTypes.ConversationUpdate)
			{
				// Handle conversation state changes, like members being added and removed
				// Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
				// Not available in all channels
			}
			else if (message.Type == ActivityTypes.ContactRelationUpdate)
			{
				// Handle add/remove from contact lists
				// Activity.From + Activity.Action represent what happened
			}
			else if (message.Type == ActivityTypes.Typing)
			{
				// Handle knowing tha the user is typing
			}
			else if (message.Type == ActivityTypes.Ping)
			{
			}

			return null;
		}
	}
}