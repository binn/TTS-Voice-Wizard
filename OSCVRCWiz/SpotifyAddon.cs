﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;

namespace OSCVRCWiz
{
 
    public class SpotifyAddon 

    {
        private static string clientId = "8ed3657eca864590843f45a659ec2976"; //TTSVoiceWizard Spotify Client ID
        private static EmbedIOAuthServer _server;
        private static SpotifyClient myClient =null;
        private static PKCETokenResponse myPKCEToken = null;
        public static string lastSong = "";
        private static string globalVerifier = "";
        static bool spotifyConnect = false;
        public static string title = "";
        

        public static async Task getCurrentSongInfo(VoiceWizardWindow MainForm)
        {
            

           if(myClient==null)
            {
                myClient = new SpotifyClient(Settings1.Default.PKCEAccessToken);
            }
            if (myClient != null)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("----Token refreshed Attempt-----");
                    PKCETokenRefreshRequest refreshRequest = new PKCETokenRefreshRequest(clientId, Settings1.Default.PKCERefreshToken);
                    PKCETokenResponse refreshResponse = await new OAuthClient().RequestToken(refreshRequest);
                    myClient = new SpotifyClient(refreshResponse.AccessToken);
                    Settings1.Default.PKCERefreshToken = refreshResponse.RefreshToken;
                    Settings1.Default.PKCEAccessToken = refreshResponse.AccessToken;
                    Settings1.Default.Save();
                    System.Diagnostics.Debug.WriteLine("----Token refreshed Successful-----");

                    if (spotifyConnect == false)
                    {
                        var ot = new OutputText();
                        ot.outputLog(MainForm, "[Spotify Connected]");
                        spotifyConnect = true;

                    }
                }




                catch (APIException ex)
                {
                    System.Diagnostics.Debug.WriteLine("-----Token doesn't need to refresh-----" + ex.Response.Body.ToString());

                }
                FullTrack m_currentTrack;
                CurrentlyPlaying m_currentlyPlaying;
                m_currentlyPlaying = await myClient.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());
                title = "";
                var artist = "";
                var duration = "";
                var progress = "";
                if (m_currentlyPlaying != null)
                {
                    IPlayableItem currentlyPlayingItem = m_currentlyPlaying.Item;
                    m_currentTrack = currentlyPlayingItem as FullTrack;
                    // System.Diagnostics.Debug.WriteLine(m_currentTrack.Name);
                    
                    if (m_currentTrack != null)
                    {

                        title = m_currentTrack.Name;
                        

                        string currentArtists = string.Empty;
                        int counter = 0;
                        foreach (SimpleArtist currentArtist in m_currentTrack.Artists)
                        {
                            if (counter <= 1)
                            {
                                currentArtists += currentArtist.Name + ", ";
                            }

                            counter++;
                        }

                        artist = currentArtists.Remove(currentArtists.Length - 2, 2);
                        // AlbumLabel.Content = m_currentTrack.Album.Name;
                        progress = new TimeSpan(0, 0, 0, 0, (int)m_currentlyPlaying.ProgressMs).ToString(@"mm\:ss");
                        duration = new TimeSpan(0, 0, 0, 0, m_currentTrack.DurationMs).ToString(@"mm\:ss");
                    }
                }
                if ((lastSong != title  || MainForm.justShowTheSong == true || MainForm.rjToggleButtonPeriodic.Checked==true) && !string.IsNullOrWhiteSpace(title) && title != ""&& VoiceWizardWindow.pauseSpotify!=true)
                {
                    VoiceWizardWindow.pauseBPM = true;
                   // lastSong = title;
                    System.Diagnostics.Debug.WriteLine(title);
                    System.Diagnostics.Debug.WriteLine(artist);
                    System.Diagnostics.Debug.WriteLine(duration);


                    var theString = "Listening to '" + title + "' by '" + artist + "' " + progress + "/" + duration+ " on Spotify";
                    if (MainForm.rjToggleButtonPeriodic.Checked==true)
                    {
                        if (MainForm.rjToggleButton3.Checked == false)
                        {
                            theString = "Listening to '" + title + "' by '" + artist + "' " + progress + "/" + duration + " on Spotify";

                        }
                        if (MainForm.rjToggleButton3.Checked == true)
                        {
                            theString = "ふ Listening to '" + title + "' by '" + artist + "' " + progress + "/" + duration;
                        }

                    }
                    if (MainForm.rjToggleButtonPeriodic.Checked == false)
                    {
                        if (MainForm.rjToggleButton3.Checked == false)
                        {
                            theString = "Listening to '" + title + "' by '" + artist + "'" + " on Spotify";

                        }
                        if (MainForm.rjToggleButton3.Checked == true)
                        {
                            theString = "ふ Listening to '" + title + "' by '" + artist + "' ";
                        }

                    }



                        var ot = new OutputText();
                    if(MainForm.rjToggleButtonSpotifySpam.Checked==true)
                    {
                        Task.Run(() => ot.outputLog(MainForm, theString));
                    }        
                    Task.Run(() => ot.outputVRChat(MainForm, theString,"spotify"));
                   // lastSong = title;
                    MainForm.justShowTheSong = false;
                    
                }

               
            }

        }
        
       


       private static async Task OnErrorReceived(object sender, string error, string state)
       {
            Console.WriteLine($"Aborting authorization, error received: {error}");
            await _server.Stop();
        }


        public async Task SpotifyConnect(VoiceWizardWindow MainForm)
        {
          MainForm.getSpotify = true;
            
            System.Diagnostics.Debug.WriteLine("Connect spotify");
            /*

               // Make sure "http://localhost:5000/callback" is in your spotify application as redirect uri!
               _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
             BrowserUtil.Open(request.ToUri());

               */
            _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
            await _server.Start();


            _server.AuthorizationCodeReceived += GetCallback;// for PKCE aka best method because you dont have to pass a clientSecret
            _server.ErrorReceived += OnErrorReceived;


            var (verifier, challenge) = PKCEUtil.GenerateCodes();
            globalVerifier = verifier;

            var loginRequest = new LoginRequest(_server.BaseUri, clientId,LoginRequest.ResponseType.Code)
            {
                CodeChallengeMethod = "S256",
                CodeChallenge = challenge,
                Scope = new[] { Scopes.UserReadCurrentlyPlaying }
            };
             BrowserUtil.Open(loginRequest.ToUri());
        }

        // This method should be called from your web-server when the user visits "http://localhost:5000/callback"
        public static async Task GetCallback(object sender, AuthorizationCodeResponse response)
        {
            System.Diagnostics.Debug.WriteLine("Getcallback code: " + response.Code.ToString());
            // Note that we use the verifier calculated above!
             var initialResponse = await new OAuthClient().RequestToken(new PKCETokenRequest(clientId, response.Code, new Uri("http://localhost:5000/callback"), globalVerifier));

            Settings1.Default.PKCERefreshToken = initialResponse.RefreshToken;
            Settings1.Default.PKCEAccessToken= initialResponse.AccessToken;
            Settings1.Default.Save();



        }





    }
}
