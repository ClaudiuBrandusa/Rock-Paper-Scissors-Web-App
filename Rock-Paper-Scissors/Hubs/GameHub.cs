using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rock_Paper_Scissors.Data;
using Rock_Paper_Scissors.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rock_Paper_Scissors.Hubs
{
    public class GameHub : Hub
    {
        IServiceProvider _sp;
        static HashSet<ConnectionData> CurrentConnections = new HashSet<ConnectionData>(); // here we are storing all of the connected clients' connections id
        static HashSet<ConnectionData> LostConnections = new HashSet<ConnectionData>(); // there goes all disconnected connections and will be removed after 3 seconds of waiting for reconnection

        static HashSet<GameModel> CurrentGames = new HashSet<GameModel>(); // here we are storing all of the current games

        public GameHub(IServiceProvider sp)
        {
            _sp = sp;            
        }

        public Task ListUsers()
        {
            return Clients.Caller.SendAsync("ListUsers", GetAllActiveUsernamesExceptingTheUsername());
        }

        public override async Task OnConnectedAsync()
        {
            var connection = new ConnectionData { ConnectionId = Context.ConnectionId, UserName = Context.User.Identity.Name };

            // checking if the connected user had a recent connection
            CurrentConnections.Add(connection);
            if (IsLostConnection(Context.User.Identity.Name))
            {
                // keep in mind that we are comparing only the username of the connectionData objects
                var lostConnection = LostConnections.FirstOrDefault(c => c.Equals(connection));
                // we to update the lostConnection's playerModel with the new connection
                // so we have to check if the player was playing a game
                GameModel game = GetGameByPlayerUsername(connection.UserName); // will remain default if the client wasn't playing
                
                if(game != null)
                {
                    // then it means that the current lost connection was playing a game
                    // so we need to get it's player model
                    PlayerModel player = GetPlayerModel(connection, game);

                    // we update the player model's connection
                    player.Connection = connection;

                    // now we have to know if the player was waiting for response from the opponent
                    if (player.IsWaiting)
                    {
                        // then we send the waiting message
                        await Clients.Client(connection.ConnectionId).SendAsync("wait", true);

                        // and we set our choice to the player's response
                        await Clients.Client(connection.ConnectionId).SendAsync("setPlayersResponse", ConvertIntChoiceToString(player.Choice));

                        // also we have to init the quit game button
                        await Clients.Client(connection.ConnectionId).SendAsync("initQuitButton");
                    }
                    else
                    {
                        // else it means that he had to submit its response or the game is over
                        if(GetGameStatus(game))
                        {
                            // then the game is over
                            // so we are setting the choices to the players
                            PlayerModel opponent = GetOpponentFromGame(player, game);
                            // for the current player
                            await Clients.Client(connection.ConnectionId).SendAsync("setPlayersResponse", ConvertIntChoiceToString(player.Choice));
                            await Clients.Client(connection.ConnectionId).SendAsync("setOpponentsResponse", ConvertIntChoiceToString(opponent.Choice));
                            // for the opponent
                            await Clients.Client(opponent.Connection.ConnectionId).SendAsync("setPlayersResponse", ConvertIntChoiceToString(opponent.Choice));
                            await Clients.Client(opponent.Connection.ConnectionId).SendAsync("setOpponentsResponse", ConvertIntChoiceToString(player.Choice));
                        }
                        else
                        {
                            // the game is still running
                            // so we are just initing the game interface
                            await Clients.Client(connection.ConnectionId).SendAsync("gameInit");
                        }
                    }
                }
                LostConnections.Remove(lostConnection);
            } 
            await Clients.All.SendAsync("UserConnected");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            var connection = CurrentConnections.FirstOrDefault(c => c.ConnectionId.Equals(Context.ConnectionId));
            if(connection != null)
            {
                //CurrentConnections.Remove(connection);
                Task task = ToBeRemoved(connection);
                task.Start();
            }
            //Context.ConnectionId);
            await Clients.Caller.SendAsync("alert",ex);
            await base.OnDisconnectedAsync(ex);
        }


        Task ToBeRemoved(ConnectionData connectionData)
        {
            if(CurrentConnections.Contains(connectionData))
            {
                CurrentConnections.Remove(connectionData);
                LostConnections.Add(connectionData);
            }

            Thread.Sleep(1500); // we are waiting for second and a half before executing the next commands

            if(LostConnections.Contains(connectionData)) // if the user hasn't connected till now
            { // then we can remove it
                LostConnections.Remove(connectionData);
                if(IsPlaying(connectionData))
                {
                    // then we have to send to the opponent the end game message
                    // but firstly have to know which player from the game is (player 1 or 2)
                    GameModel game = GetGameByPlayerUsername(connectionData.UserName);
                    // we find out who the opponent is
                    ConnectionData opponent = GetOpponentFromGame(connectionData, game).Connection;
                    // then we notice opponent the fact that the game has ended
                    Clients.Client(opponent.ConnectionId).SendAsync("endGame");
                }
                return Clients.All.SendAsync("UserDisconnected", connectionData.UserName);
            }// else the player has reconnected
            return Task.CompletedTask;
        }

        public List<string> GetAllActiveUsernames()
        {
            List<string> activeUsers = new List<string>();

            foreach(var connectionData in CurrentConnections)
            {
                activeUsers.Add(connectionData.UserName);
            }

            return activeUsers;
        }

        public List<string> GetAllActiveUsernamesExceptingTheUsername(string username="")
        {
            // username as an empty string means that we are ignoring the identitiy's username
            if(string.IsNullOrEmpty(username))
            {
                username = Context.User.Identity.Name;
            }

            List<string> activeUsers = new List<string>();

            foreach (var connectionData in CurrentConnections)
            {
                if(username.Equals(connectionData.UserName)) // if we found the username that we want to ignore
                { 
                    // then we will jump to the next iteration
                    continue;
                }
                activeUsers.Add(connectionData.UserName);
            }
            foreach (var connectionData in LostConnections)
            {
                if (username.Equals(connectionData.UserName)) // if we found the username that we want to ignore
                {
                    // then we will jump to the next iteration
                    continue;
                }
                activeUsers.Add(connectionData.UserName);
            }
            return activeUsers;
        }

        public Task PlayWith(string username)
        {
            //Clients.Caller.SendAsync("alert", "current games lenght "+CurrentGames.Count.ToString());
            // get the player's connectionData
            ConnectionData playerConnection = CurrentConnections.FirstOrDefault(c => c.UserName.Equals(Context.User.Identity.Name));
            //Clients.Caller.SendAsync("alert","the user that you're trying to play is "+username);
            // check if we have found the player's connection
            if(playerConnection == default(ConnectionData))
            {
                // then return
                Clients.Caller.SendAsync("alert", "player not found");
                return Clients.Caller.SendAsync("endGame");
            }
            // check if is already playing with someone 
            // and if that person is not the one who we are looking for
            if (IsPlaying(playerConnection) && !IsPlayingWith(playerConnection.UserName, username)) 
            {
                // then we have to return
                Clients.Caller.SendAsync("alert", "player is busy");
                return Clients.Caller.SendAsync("endGame");
            }
            // so now we can create the player model
            PlayerModel player = new PlayerModel { Connection = playerConnection };
            // get the opponent's connectionData
            ConnectionData opponentConnection = CurrentConnections.FirstOrDefault(c => c.UserName.Equals(username));
            // check if we have found the opponent's connectionData
            if(opponentConnection == default(ConnectionData))
            {
                // then we return
                Clients.Caller.SendAsync("alert", "opponent not found"); //Task.CompletedTask;
                return Clients.Caller.SendAsync("endGame");
            }
            // check if the opponent's connectionId is still working
            //if(opponentConnection.ConnectionId)
            // check if is already playing with someone
            // and if that person is not the one who we are looking for
            if (IsPlaying(opponentConnection) && !IsPlayingWith(username, playerConnection.UserName))
            {
                // then we return
                Clients.Caller.SendAsync("alert", "opponent is busy");
                return Clients.Caller.SendAsync("endGame");
            }
            // we are creating the opponent's player model
            PlayerModel opponent = new PlayerModel { Connection = opponentConnection };
            // now that we found the connections then we can create the game model
            // but firstly we want check if it's a play again request
            GameModel game = GetGameByPlayersUsername(username, playerConnection.UserName);
            if (game == default)
            {
                // then there is not a play again request
                game = new GameModel() { Player1 = player, Player2 = opponent };
                // add the game to the list
                CurrentGames.Add(game);
            }
            else
            {
                // if the game isn't over
                if(!GetGameStatus(game))
                {
                    // then there is nothing we can do
                    return Task.CompletedTask;
                }
                // else it's a play again request
                // so we will have to reset the players' responses
                Clients.Client(playerConnection.ConnectionId).SendAsync("resetGame");
                Clients.Client(opponentConnection.ConnectionId).SendAsync("resetGame");
            }
            // init the game
            // for the opponent
            Clients.Client(opponentConnection.ConnectionId).SendAsync("gameInit");
            // for the client
            return Clients.Caller.SendAsync("gameInit");
        }

        public bool PlayAgain(string username)
        {
            // we have to get it's connection in order to validate the username
            ConnectionData connection = GetConnectionDataByUsername(username);
            if(connection == default)
            {
                // then there is no user with the username we are looking for
                return false;
            }
            // now that we have found the connection
            // we need to know if this player is not involved in another game
            if(IsPlaying(connection))
            {
                return false;
            }
            return true;
        }

        public bool Choose(int choice)
        {
            // choices: 0 - rock, 1 - paper, 2 - scissors
            if(choice < 0 || choice > 2)
            {
                return false;
            }
            // getting the game model
            GameModel game = GetGameByPlayerUsername(Context.User.Identity.Name);
            // getting the player's model
            PlayerModel player = GetPlayerModel(Context.User.Identity.Name, game);
            // with the current game we can get the opponent's player model
            PlayerModel opponent = GetOpponentFromGame(player, game);

            // we set the player's choice
            player.Choice = choice;
            player.Chose = true;

            if (opponent.IsWaiting) // if the opponent already chose
            {
                opponent.IsWaiting = false;
                // then we can continue and send a result
                // so firstly we have to stop waiting on the opponent's client
                StopWaiting(opponent.Connection.UserName);
                // and then we have to decide who won
                if (game.Player1.Choice == game.Player2.Choice)
                {
                    // then it's a draw
                    Clients.Client(opponent.Connection.ConnectionId).SendAsync("draw");
                    Clients.Caller.SendAsync("draw");
                }
                else
                {
                    bool status = Decide(player, game);
                    using (var scope = _sp.CreateScope())
                    {
                        using(var context = scope.ServiceProvider.GetRequiredService<AppDbContext>())
                        {
                            var playerData = context.Users.FirstOrDefault(u => u.UserName.Equals(player.Connection.UserName));
                            var opponentData = context.Users.FirstOrDefault(u => u.UserName.Equals(opponent.Connection.UserName));
                            if (status)
                            {
                                Clients.Caller.SendAsync("win");
                                // playerWins++

                                if (playerData != null)
                                {
                                    playerData.GamesWon++;
                                }

                                // opponentLosses++
                                if (opponentData != null)
                                {
                                    opponentData.GamesLost++;
                                }

                                Clients.Client(opponent.Connection.ConnectionId).SendAsync("loss");
                            }
                            else
                            {
                                // opponentWins++
                                if (opponentData != null)
                                {
                                    opponentData.GamesWon++;
                                }
                                Clients.Client(opponent.Connection.ConnectionId).SendAsync("win");
                                // playerLosses++
                                if (playerData != null)
                                {
                                    playerData.GamesLost++;
                                }
                                Clients.Caller.SendAsync("loss");
                            }
                            context.SaveChangesAsync();
                        }
                    }
                }
                // next we have to show what each one chose
                Clients.Client(opponent.Connection.ConnectionId).SendAsync("setOpponentsChoice", ConvertIntChoiceToString(choice));
                Clients.Caller.SendAsync("setOpponentsChoice", ConvertIntChoiceToString(opponent.Choice));
                Clients.Caller.SendAsync("setClientsChoice", ConvertIntChoiceToString(choice));

                // initing the play again button for
                // player
                Clients.Caller.SendAsync("initPlayAgainButton", opponent.Connection.UserName);
                // opponent
                Clients.Client(opponent.Connection.ConnectionId).SendAsync("initPlayAgainButton", player.Connection.UserName);
                return true;
            }
            player.IsWaiting = true;

            Clients.Caller.SendAsync("setClientsChoice", ConvertIntChoiceToString(choice));
            Clients.Caller.SendAsync("Wait", true);
            return true;
        }

        public Task StopWaiting(string username="")
        {
            if(string.IsNullOrEmpty(username))
            {
                username = Context.User.Identity.Name;
            }
            PlayerModel player = GetPlayerModel(username);
            player.IsWaiting = false;
            return Clients.Caller.SendAsync("Wait", false);
        }

        public Task EndGame()
        {
            // getting the player connection data
            ConnectionData player = GetConnectionDataByUsername(Context.User.Identity.Name);
            // check if we have found the data
            if (player == default(ConnectionData))
            {
                // then return
                return Task.CompletedTask;
            }
            // finding the game
            GameModel game = GetGameByPlayerUsername(player.UserName);
            // if we haven't found the game
            if (game == default(GameModel))
            {
                // then we return
                return Task.CompletedTask;
            }

            // setting the opponent's connectionId
            string opponent = GetOpponentFromGame(player, game).Connection.ConnectionId;
            // removing the game object
            CurrentGames.Remove(game);
            Clients.Client(opponent).SendAsync("endGame");
            return Clients.Caller.SendAsync("endGame");
        }

        // static methods

        public static bool IsOnline(string username)
        {
            return CurrentConnections.FirstOrDefault(c => c.UserName.Equals(username)) != default(ConnectionData) || IsLostConnection(username);
        }

        // checks if the connection with the username had been lost
        public static bool IsLostConnection(string username)
        {
            return LostConnections.FirstOrDefault(c => c.UserName.Equals(username)) != default(ConnectionData);
        }

        public static bool GetGameStatus(GameModel game) // true - the game is over | false - the game is still running
        {
            // if the players have made their choice then the game is over
            return game.Player1.Chose && game.Player2.Chose;
        }

        public static bool IsPlaying(string username)
        {
            // true if we get a game model who contains a player with this name
            return GetGameByPlayerUsername(username) != default;
        }

        public static bool IsPlaying(ConnectionData connection)
        {
            return IsPlaying(connection.UserName);
        }

        public static bool IsPlayingWith(string username, string opponent)
        {
            // true if we get a game model who contains a player with this name
            return GetGameByPlayersUsername(username, opponent) != default;
        }
        public static bool UserExist(string username)
        {
            return CurrentConnections.FirstOrDefault(c => c.UserName.Equals(username)) != default ? true : LostConnections.FirstOrDefault(c => c.UserName.Equals(username)) != default;
        }

        public static ConnectionData GetConnectionDataByUsername(string username)
        {
            ConnectionData connection = CurrentConnections.FirstOrDefault(c => c.UserName.Equals(username));
            return connection != default(ConnectionData) ? connection : LostConnections.FirstOrDefault(c => c.UserName.Equals(username));
        }

        public static GameModel GetGameByPlayersUsername(string player1, string player2)
        {
            GameModel game = CurrentGames.FirstOrDefault(g => g.Player1.Connection.UserName.Equals(player1) && g.Player2.Connection.UserName.Equals(player2));
            return game != default(GameModel) ? game : CurrentGames.FirstOrDefault(g => g.Player1.Connection.UserName.Equals(player2) && g.Player2.Connection.UserName.Equals(player1));
        }
        public static GameModel GetGameByPlayerUsername(string player)
        {
            return CurrentGames.FirstOrDefault(g => g.Player1.Connection.UserName.Equals(player) || g.Player2.Connection.UserName.Equals(player));
        }

        public static PlayerModel GetOpponentFromGame(PlayerModel player, GameModel game)
        {
            PlayerModel opponent = default;
            if (game.Player1.Connection.Equals(player.Connection)) // if the caller is the player 1
            {
                // then the opponent is the player 2
                opponent = game.Player2;
            }
            else
            {
                // else the opponent is the player 1
                opponent = game.Player1;
            }
            return opponent;
        }

        public static PlayerModel GetOpponentFromGame(ConnectionData player, GameModel game)
        {
            if (game.Player1.Connection.Equals(player)) // if the caller is the player 1
            {
                // then the opponent is the player 2
                return game.Player2;
            }
            else if(game.Player2.Connection.Equals(player))
            {
                // else the opponent is the player 1
                return game.Player1;
            }else
            {
                return default;
            }
        }

        public static PlayerModel GetPlayerModel(ConnectionData connection, GameModel game)
        {
            if(game.Player1.Connection.Equals(connection))
            {
                return game.Player1;
            }else if(game.Player2.Connection.Equals(connection))
            {
                return game.Player2;
            }else
            {
                return default;
            }
        }

        public static PlayerModel GetPlayerModel(string username, GameModel game)
        {
            if (game.Player1.Connection.UserName.Equals(username))
            {
                return game.Player1;
            }
            else if (game.Player2.Connection.UserName.Equals(username))
            {
                return game.Player2;
            }
            else
            {
                return default;
            }
        }

        public static PlayerModel GetPlayerModel(string username)
        {
            PlayerModel player = default;
            foreach(GameModel game in CurrentGames)
            {
                player = GetPlayerModel(username, game);
                if(player != default)
                {
                    break;
                }
            }
            return player;
        }

        public static bool Decide(int player, int opponent)
        {
            if((player + 1) % 3 == opponent)
            {
                return false; // player losses
            }else
            {
                return true; // player wins
            }
        }

        public static bool Decide(PlayerModel player, GameModel game)
        {
            if(game.Player1.Connection.Equals(player.Connection))
            {
                return Decide(game.Player1.Choice, game.Player2.Choice);
            }
            return Decide(game.Player2.Choice, game.Player1.Choice);
        }

        public static string ConvertIntChoiceToString(int choice)
        {
            switch (choice)
            {
                case 0:
                    return "Rock";
                case 1:
                    return "Paper";
                case 2:
                    return "Scissors";
                default:
                    return "";
            }
        }
    }
}
