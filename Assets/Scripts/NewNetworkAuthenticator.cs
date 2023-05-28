using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using Mirror;
using Mirror.Authenticators;

public class NewNetworkAuthenticator : NetworkAuthenticator
{
    [Header("UI")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text resultText;

    readonly HashSet<NetworkConnection> connectionsPendingDisconnect = new HashSet<NetworkConnection>();

    #region Server

    [Serializable]
    public struct PlayerData
    {
        public string username;
        public string password;
        public string salt;
    }

    public struct PlayersData
    {
        public List<PlayerData> playersData;
    }

    const int SALT_SIZE = 16;
    const int PBKDF2_ITERATION = 10000;

    public static byte[] GenerateSalt()
    {
        var buff = new byte[SALT_SIZE];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(buff);
        }
        return buff;
    }

    public static string GeneratePasswordHashPBKDF2(string pwd, byte[] salt)
    {
        var result = "";
        var b = new Rfc2898DeriveBytes(pwd, salt, PBKDF2_ITERATION);
        var k = b.GetBytes(32);
        result = Convert.ToBase64String(k);
        return result;
    }

    private List<PlayerData> playersData;

    void LoadPlayersData()
    {
        string playersDataJsonPath = Path.Combine(Application.persistentDataPath, "server/players.json");
        if (!File.Exists(playersDataJsonPath))
            playersData = new List<PlayerData>();
        else
        {
            string json = File.ReadAllText(playersDataJsonPath);
            playersData = JsonUtility.FromJson<PlayersData>(json).playersData;
        }
    }

    void SavePlayersData()
    {
        string directoryPath = Path.Combine(Application.persistentDataPath, "server/");
        string playersDataJsonPath = Path.Combine(directoryPath, "players.json");
        string json = JsonUtility.ToJson(new PlayersData { playersData = playersData });
        Directory.CreateDirectory(directoryPath);
        File.WriteAllText(playersDataJsonPath, json);
    }

    /// <summary>
    /// Called on server from StartServer to initialize the Authenticator
    /// <para>Server message handlers should be registered in this method.</para>
    /// </summary>
    public override void OnStartServer()
    {
        // register a handler for the authentication request we expect from client
        NetworkServer.RegisterHandler<BasicAuthenticator.AuthRequestMessage>(OnAuthRequestMessage, false);

        LoadPlayersData();
    }

    /// <summary>
    /// Called on server from StopServer to reset the Authenticator
    /// <para>Server message handlers should be unregistered in this method.</para>
    /// </summary>
    public override void OnStopServer()
    {
        // unregister the handler for the authentication request
        NetworkServer.UnregisterHandler<BasicAuthenticator.AuthRequestMessage>();
    }

    /// <summary>
    /// Called on server when the client's AuthRequestMessage arrives
    /// </summary>
    /// <param name="conn">Connection to client.</param>
    /// <param name="msg">The message payload</param>
    public void OnAuthRequestMessage(NetworkConnectionToClient conn, BasicAuthenticator.AuthRequestMessage msg)
    {
        if (connectionsPendingDisconnect.Contains(conn)) return;

        // check the credentials
        bool isAuthenticated = false;
        if (!string.IsNullOrWhiteSpace(msg.authUsername) &&
            !string.IsNullOrEmpty(msg.authPassword))
        {
            bool createAccount = true;
            for (int i = 0; i < playersData.Count; i++)
                if (msg.authUsername == playersData[i].username)
                {
                    createAccount = false;

                    var salt = Convert.FromBase64String(playersData[i].salt);
                    var hash = GeneratePasswordHashPBKDF2(msg.authPassword, salt);

                    if (hash == playersData[i].password)
                        isAuthenticated = true;

                    break;
                }

            // Create an account when logged in with a non-existent user name
            if (createAccount)
            {
                var salt = GenerateSalt();
                var hash = GeneratePasswordHashPBKDF2(msg.authPassword, salt);

                isAuthenticated = true;
                playersData.Add(new PlayerData
                {
                    username = msg.authUsername,
                    password = hash,
                    salt = Convert.ToBase64String(salt),
                });
                SavePlayersData();
            }
        }

        if (isAuthenticated)
        {
            conn.authenticationData = msg.authUsername;

            // create and send msg to client so it knows to proceed
            BasicAuthenticator.AuthResponseMessage authResponseMessage = new BasicAuthenticator.AuthResponseMessage
            {
                code = 100,
                message = "Success"
            };

            conn.Send(authResponseMessage);

            // Accept the successful authentication
            ServerAccept(conn);
        }
        else
        {
            connectionsPendingDisconnect.Add(conn);

            // create and send msg to client so it knows to disconnect
            BasicAuthenticator.AuthResponseMessage authResponseMessage = new BasicAuthenticator.AuthResponseMessage
            {
                code = 200,
                message = "Invalid Credentials"
            };

            conn.Send(authResponseMessage);

            // must set NetworkConnection isAuthenticated = false
            conn.isAuthenticated = false;

            // disconnect the client after 1 second so that response message gets delivered
            StartCoroutine(DelayedDisconnect(conn, 1f));
        }
    }

    IEnumerator DelayedDisconnect(NetworkConnectionToClient conn, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        // Reject the unsuccessful authentication
        ServerReject(conn);

        yield return null;

        // remove conn from pending connections
        connectionsPendingDisconnect.Remove(conn);
    }

    #endregion

    #region Client

    /// <summary>
    /// Called on client from StartClient to initialize the Authenticator
    /// <para>Client message handlers should be registered in this method.</para>
    /// </summary>
    public override void OnStartClient()
    {
        // register a handler for the authentication response we expect from server
        NetworkClient.RegisterHandler<BasicAuthenticator.AuthResponseMessage>(OnAuthResponseMessage, false);

        resultText.text = "";
    }

    /// <summary>
    /// Called on client from StopClient to reset the Authenticator
    /// <para>Client message handlers should be unregistered in this method.</para>
    /// </summary>
    public override void OnStopClient()
    {
        // unregister the handler for the authentication response
        NetworkClient.UnregisterHandler<BasicAuthenticator.AuthResponseMessage>();
    }

    /// <summary>
    /// Called on client from OnClientConnectInternal when a client needs to authenticate
    /// </summary>
    public override void OnClientAuthenticate()
    {
        BasicAuthenticator.AuthRequestMessage authRequestMessage = new BasicAuthenticator.AuthRequestMessage
        {
            authUsername = usernameInput.text,
            authPassword = passwordInput.text
        };

        NetworkClient.Send(authRequestMessage);
    }

    /// <summary>
    /// Called on client when the server's AuthResponseMessage arrives
    /// </summary>
    /// <param name="msg">The message payload</param>
    public void OnAuthResponseMessage(BasicAuthenticator.AuthResponseMessage msg)
    {
        if (msg.code == 100)
        {
            // Authentication has been accepted
            ClientAccept();
        }
        else
        {
            Debug.LogError($"Authentication Response: {msg.message}");

            // Authentication has been rejected
            ClientReject();

            resultText.text = "パスワードが間違っています";
        }
    }

    #endregion
}
