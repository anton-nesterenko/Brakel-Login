using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using PushNotifications;
using System.Net.Sockets;
using System.Threading;

namespace BrakelInlogApplication
{
	/// <summary>
	/// The actual implementation of the API calls happens in this helper class
	/// </summary>
	public sealed class APIHelper
	{
		/// <summary>
		/// The private instance for easy instantiation
		/// </summary>
		private static readonly APIHelper _instance = new APIHelper();

		/// <summary>
		/// Private to prevent instantiation
		/// </summary>
		private APIHelper()
		{
			//Register event handler for background polling
			BackgroundPoller.Instance.OnResultChanged += OnPollingResult;
		}

		/// <summary>
		/// The Instance of this class
		/// </summary>
		public static APIHelper Instance
		{
			get { return _instance; }
		}

		/// <summary>
		/// Validates the user's credentials and returns a token that will be used to validate other requests
		/// </summary>
		/// <param name="username">The username</param>
		/// <param name="passwordHash">The hashed password</param>
		/// <param name="device">The deviceId of the iPhone or iPad</param>
		/// <returns>A token not equal to all 0 on succes, a token of all 0 on failure</returns>
		public Guid Login(string username, string passwordHash, string device)
		{
			Guid userToken = Guid.Empty;

			using (var connection = new SqlConnection(ConstantHelper.ConnectionString))
			{
				connection.Open();

				// perform work with connection
				string query = String.Format("SELECT [hash] FROM [user] WHERE [username] = '{0}'", username);
				var command = new SqlCommand(query, connection);
				var sqlHash = command.ExecuteScalar() as String;

				//validate credentials
				if (passwordHash.Equals(sqlHash, StringComparison.OrdinalIgnoreCase))
				{
					//generate token
					userToken = Guid.NewGuid();

					//register token in db to the user
					query = String.Format("INSERT INTO [token] ([username], [token], [deviceId], [createDateTime]) VALUES('{0}','{1}','{2}','{3}')",
					                     username, userToken, device, DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
					command = new SqlCommand(query, connection);
					int result = command.ExecuteNonQuery();
					if (result < 1)
					{
						userToken = Guid.Empty;
					}
				}
			}

			//return id
			return userToken;
		}

		/// <summary>
		/// Returns a list of buildngs which the current user can see, and the permissions he has in those buildings
		/// </summary>
		/// <param name="userToken">The current user's token</param>
		/// <returns>The list of Buildings</returns>
		public List<Building> GetBuildings(Guid userToken)
		{
			var buildings = new List<Building>();

			using (var connection = new SqlConnection(ConstantHelper.ConnectionString))
			{
				connection.Open();
				string query = String.Format("SELECT [username] FROM [token] WHERE [token] = '{0}'", userToken);
				var command = new SqlCommand(query, connection);

				//Validate token
				var username = command.ExecuteScalar() as String;
				if (String.IsNullOrWhiteSpace(username))
				{
					throw new APIException("The provided userToken is invalid or has expired", "userToken");
				}
				else
				{
					//All this trouble for a single bit?
					query = String.Format(@"SELECT	[building].*, [userBuildingCouple].[accessRights],
												CAST((SELECT MAX(CAST([hasAlarm] AS int)) FROM [room] WHERE [room].[buildingId] IN
													(SELECT [buildingId] FROM [building] WHERE [building].[parentId]= [userBuildingCouple].[buildingId]))
												AS bit) AS [hasAlarm]
											FROM [building]
											LEFT JOIN [userBuildingCouple] ON [building].[buildingId] = [userBuildingCouple].[buildingId]
											LEFT JOIN [user] ON [user].[userId] = [userBuildingCouple].[userId]
											WHERE [user].[username] = '{0}'", username);
					command = new SqlCommand(query, connection);

					//Fill collection
					SqlDataReader reader = command.ExecuteReader();
					while (reader.Read())
					{
						buildings.Add(new Building
						{
					  		AccessRole = Building.ParseAccessRightsFromString(reader["accessRights"].ToString()),
						  	BuildingID = Int32.Parse(reader["buildingId"].ToString()),
						 	BuildingName = reader["name"].ToString(),
							Parent = Int32.Parse(reader["parentId"].ToString()),
							HasAlarm = Boolean.Parse(reader["hasAlarm"].ToString())
						});
					}
				}
			}
			//return collection
			return buildings;
		}

		/// <summary>
		/// Method to iniate making changes to groups
		/// </summary>
		/// <param name="userToken">The user token</param>
		/// <param name="buildingId">The building id for the building in which the groups are</param>
		/// <param name="changes">The list of changes you want to commit</param>
		/// <returns>The list of changes with a boolean value to indicate succes of the operation per change</returns>
		public List<Changes> MakeChangesToGroups(Guid userToken, int buildingId, List<Changes> changes)
		{
			if (userToken != Guid.Empty)
			{
				if (buildingId != 0)
				{
					JObject result = null;
					TcpClient socket = null;
					NetworkStream stream = null;
					try
					{
						#region Convert objects to JSON
						string requestBody = "";
						changes.ForEach(
							i => requestBody += ("," + i.ToJSONString())
							);
						if (requestBody.Length > 1)
							requestBody = requestBody.Substring(1);
						requestBody = @"{""changes"":[" + requestBody + "]}\n";
						byte[] byte1 = Encoding.ASCII.GetBytes(requestBody);
						#endregion
						#region Make Request
						string targetBuilding = Building.GetBuildingIp(buildingId);
						string host = targetBuilding.Split (':')[0];
						int port = Int32.Parse (targetBuilding.Split (':')[1]);

						socket = new TcpClient(host, port);
						socket.SendTimeout = ConstantHelper.BuildingTimeout;
						socket.ReceiveTimeout = ConstantHelper.BuildingTimeout;

						stream = socket.GetStream();
						stream.Write(byte1, 0, byte1.Length);
						stream.Flush();

						byte[] buff = new byte[2048];
						int bytesRead = stream.Read(buff, 0, buff.Length);
						string resultString = Encoding.ASCII.GetString(buff, 0, bytesRead);

						Debug.WriteLine(resultString);
						result = JObject.Parse(resultString);
						#endregion
					}
					catch (Exception ex)
					{
						Debug.WriteLine(ex.Message);
						changes.ForEach(
							i => i.ChangeStatus = false
						);
						return changes;
					}
					finally
					{
						if(stream != null)
							stream.Close();
						if(socket != null)
							socket.Close();
					}

					var changesArray = result["changes"] as JArray;

					//parse json and update changes
					foreach (JToken item in changesArray)
					{
						Changes ch = changes.FirstOrDefault(i => i.GroupID.ToString().Equals(item["GroupID"].ToString()));
						if (ch != default(Changes))
						{
							ch.ChangeStatus = Boolean.Parse(item["ChangeStatus"].ToString() ?? Boolean.FalseString);
						}
					}

					//Start background polling					
					BackgroundPoller.Instance.StartPollingBuilding(userToken, buildingId);

					//return initial result
					return changes;
				}
				else
				{
					throw new APIException("The provided buildingId is invalid", "buildingId");
				}
			}
			else
			{
				throw new APIException("The provided userToken is invalid or expired", "userToken");
			}
		}

		/// <summary>
		/// Get the current user's screen layout for the selected building
		/// </summary>
		/// <param name="userToken">The current user's token</param>
		/// <param name="buildingId">The building for which you want the layout</param>
		/// <returns>A string representation of the XML, which describes the layout of the application</returns>
		public string GetUserLayout(Guid userToken, int buildingId)
		{
			string resultLayout = "";

			using (var connection = new SqlConnection(ConstantHelper.ConnectionString))
			{
				connection.Open();
				string query = String.Format("SELECT [username] FROM [token] WHERE [token] = '{0}'", userToken);
				var command = new SqlCommand(query, connection);

				//Validate token
				var username = command.ExecuteScalar() as String;
				if (String.IsNullOrWhiteSpace(username))
				{
					throw new APIException("The provided userToken is invalid or has expired", "userToken");
				}
				else
				{
					//get the layout for the user - building combination
					query = String.Format(@"SELECT	[userBuildingCouple].[screenLayout] FROM [userBuildingCouple]													
													LEFT JOIN [user] ON [user].[userId] = [userBuildingCouple].[userId]
											WHERE	[user].[username] = '{0}' and [userBuildingCouple].[buildingId] = {1}",
					                      username, buildingId);
					command = new SqlCommand(query, connection);

					//get the value and store it in the string
					SqlDataReader reader = command.ExecuteReader();
					if (reader.HasRows)
					{
						while (reader.Read())
						{
							resultLayout = reader["screenLayout"].ToString();
						}
					}
					else
					{
						throw new APIException("The provided buildingId is invalid", "buildingId");
					}
				}
			}

			//return layout json as a string
			return resultLayout;
		}

		/// <summary>
		/// Eventhandler for the polling result change
		/// </summary>
		/// <param name="userToken">The userToken of the user who initiated the poll</param>
		/// <param name="buildingId">The building this result is about</param>
		/// <param name="json">The result in JSON format</param>
		public static void OnPollingResult (Guid userToken, int buildingId, string json)
		{
			string deviceID = "";
			using (var connection = new SqlConnection(ConstantHelper.ConnectionString)) {
				connection.Open ();

				string query = String.Format ("SELECT [deviceId] FROM [token] WHERE [userToken] = '{0}'", userToken);
				var command = new SqlCommand (query, connection);
				deviceID = command.ExecuteScalar () as String;
			}
			if (!String.IsNullOrWhiteSpace (deviceID)) {
				string message = String.Format (@"{{ ""building"":{0}, ""changes"": {1} }}", buildingId, json);
				PushNotification.SendPushNotification (deviceID, message);
			}
		}

		/// <summary>
		/// Get all floors in the building and the accessrights the user has in those rooms
		/// </summary>
		/// <param name="userToken">The current user's token</param>
		/// <param name="buildingId">The building id</param>
		/// <param name="getRoomsRecursivly">boolean to indicate if rooms should be retrieved recursivly, or ignored</param>
		/// <returns>The list of Floors</returns>
		public List<Floor> GetFloors(Guid userToken, int buildingId, bool getRoomsRecursivly)
		{
			var floors = new List<Floor>();

			using (var connection = new SqlConnection(ConstantHelper.ConnectionString))
			{
				connection.Open();
				string query = String.Format("SELECT [username] FROM [token] WHERE [token] = '{0}'", userToken);
				var command = new SqlCommand(query, connection);

				//Validate token
				var username = command.ExecuteScalar() as String;
				if (String.IsNullOrWhiteSpace(username))
				{
					throw new APIException("The provided userToken is invalid or has expired", "userToken");
				}
				else
				{
					//SUBSELECT FOR A SINGLE BIT!
					query = String.Format(@"SELECT	[building].*, [userBuildingCouple].[accessRights],
												CAST((SELECT MAX(CAST([hasAlarm] AS int)) FROM [room] WHERE [room].[buildingId] = [building].[buildingId])
												AS bit) AS [hasAlarm]
											FROM [building]
											LEFT JOIN [userBuildingCouple] ON [building].[parentId] = [userBuildingCouple].[buildingId]
											LEFT JOIN [user] ON [user].[userId] = [userBuildingCouple].[userId]
											WHERE	[user].[username] = '{0}' and [building].[parentId] = {1}", username, buildingId);
					command = new SqlCommand(query, connection);



					//Fill collection
					SqlDataReader reader = command.ExecuteReader();
					while (reader.Read())
					{
						var floor = new Floor
						{
							AccessRole = Building.ParseAccessRightsFromString(reader["accessRights"].ToString()),
							BuildingID = Int32.Parse(reader["buildingId"].ToString()),
							BuildingName = reader["name"].ToString(),
							Parent = Int32.Parse(reader["parentId"].ToString()),
							HasAlarm = Boolean.Parse(reader["hasAlarm"].ToString())
						};
						if (getRoomsRecursivly)
						{
							floor.Rooms = GetRooms(userToken, floor.BuildingID);
						}
						floors.Add(floor);
					}
				}
			}
			//return collection
			return floors;
		}

		/// <summary>
		/// Get all rooms on a certain floor
		/// </summary>
		/// <param name="userToken">The current user's token</param>
		/// <param name="floorId">The floor id</param>
		/// <returns>The list of rooms</returns>
		public List<Room> GetRooms(Guid userToken, int floorId)
		{
			var rooms = new List<Room>();

			using (var connection = new SqlConnection(ConstantHelper.ConnectionString))
			{
				connection.Open();
				string query = String.Format("SELECT [username] FROM [token] WHERE [token] = '{0}'", userToken);
				var command = new SqlCommand(query, connection);

				//Validate token
				var username = command.ExecuteScalar() as String;
				if (String.IsNullOrWhiteSpace(username))
				{
					throw new APIException("The provided userToken is invalid or has expired", "userToken");
				}
				else
				{
					//join buildings on users rights?
					query = String.Format(@"SELECT	[room].* FROM [room]
													LEFT JOIN [building] ON [room].[buildingId] = [building].[buildingId]
											WHERE	[building].[buildingId] = {0}", floorId);
					command = new SqlCommand(query, connection);

					//Fill collection
					SqlDataReader reader = command.ExecuteReader();
					while (reader.Read())
					{
						rooms.Add(new Room()
						{
							RoomID = Int32.Parse(reader["roomId"].ToString()),
							RoomName = reader["roomName"].ToString(),
							BuildingID = Int32.Parse(reader["buildingId"].ToString()),
							XCoordinate = Int32.Parse(reader["xCoordinate"].ToString()),
							YCoordinate = Int32.Parse(reader["yCoordinate"].ToString()),
							Width = Int32.Parse(reader["width"].ToString()),
							Height = Int32.Parse(reader["height"].ToString()),
							IsEnabled = Boolean.Parse(reader["enabled"].ToString()),
							HasAlarmValue = Boolean.Parse(reader["hasAlarm"].ToString())
						});
					}
				}
			}
			//return collection
			return rooms;
		}

		public List<Changes> GetGroups (Guid userToken, int buildingId)
		{
			List<Changes> changes = new List<Changes>();
			var r = new Random();
			var max = r.Next(2, 5);
			for (uint j = 0; j < max; j++)
			{
				changes.Add(new Changes { GroupID = (1 + j), GroupName = String.Format("Sturingsgroep {0}", 1 + j) , ChangeValue = (uint)r.Next(0, 255) });
			}
			return changes;
		}
	}
}