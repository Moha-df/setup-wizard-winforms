using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.WinForms;

namespace setup_wizard.Panels
{
    public partial class FinishPanel : UserControl
    {
		private Button btnMute;
        private Button btnFinish;
		private Panel videoPanel;
		private WebView2? videoPlayer;
		private bool isMuted = false; // Son activé par défaut

        		public FinishPanel()
		{
			InitializeComponent();
			this.VisibleChanged += FinishPanel_VisibleChanged;
			this.Load += FinishPanel_Load;
			this.Paint += FinishPanel_Paint;
		}

		private void FinishPanel_VisibleChanged(object sender, EventArgs e)
		{
			if (this.Visible)
			{
				StartVideo();
			}
		}

		private void FinishPanel_Load(object sender, EventArgs e)
		{
			StartVideo();
		}

		private void FinishPanel_Paint(object sender, PaintEventArgs e)
		{
			// Se déclenche une seule fois pour éviter les appels multiples
			this.Paint -= FinishPanel_Paint;
			StartVideo();
		}

		private void StartVideo()
		{
			// Éviter les appels multiples
			if (videoPlayer != null) return;

			Task.Delay(200).ContinueWith(_ =>
			{
				if (this.InvokeRequired)
				{
					this.Invoke(new Action(() => PlayVideo()));
				}
				else
				{
					PlayVideo();
				}
			});
		}

        		private void InitializeComponent()
		{
			// Video Panel - Centré et stylé
			videoPanel = new Panel
			{
				Location = new Point(80, 10),
				Size = new Size(540, 304), // Format 16:9 (540x304)
				BorderStyle = BorderStyle.None, // Pas de bordure
				BackColor = Color.Transparent // Fond transparent
			};
			this.Controls.Add(videoPanel);

			// Mute Button - Repositionné pour la nouvelle vidéo
			btnMute = new Button
			{
				Text = "🔇 Mute", // Son activé par défaut, donc bouton "Mute"
				Font = new Font("Segoe UI", 12F, FontStyle.Bold),
				Location = new Point(80, 330),
				Size = new Size(120, 35),
				BackColor = Color.DodgerBlue,
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat
			};
			btnMute.Click += (s, e) => ToggleMute();
			this.Controls.Add(btnMute);

			// Finish Button - Repositionné pour la nouvelle vidéo
			btnFinish = new Button
			{
				Text = "Terminer",
				Font = new Font("Segoe UI", 12F, FontStyle.Bold),
				Location = new Point(500, 330),
				Size = new Size(120, 35),
				BackColor = Color.LimeGreen,
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat
			};
			btnFinish.Click += btnFinish_Click;
			this.Controls.Add(btnFinish);
		}

		private void ToggleMute()
		{
			isMuted = !isMuted;
			if (isMuted)
			{
				btnMute.Text = "🔊 Unmute";
				SetVideoMute(true);
			}
			else
			{
				btnMute.Text = "🔇 Mute";
				SetVideoMute(false);
			}
		}

		private async void SetVideoMute(bool muted)
		{
			if (videoPlayer != null)
			{
				try
				{
					await videoPlayer.ExecuteScriptAsync($@"
						(function() {{
							var video = document.getElementById('mainVideo');
							if (video) {{
								if ({muted.ToString().ToLower()}) {{
									video.volume = 0; // Son coupé
								}} else {{
									video.volume = 0.5; // Remettre à 50%
								}}
								return 'Volume: ' + video.volume;
							}}
							return 'Video not found';
						}})();
					");
				}
				catch { }
			}
		}

		private async void PlayVideo()
		{
			// Loading stylé dans le videoPanel
			var lblLoading = new Label
			{
				Text = "⏳ Chargement de la vidéo...",
				Font = new Font("Segoe UI", 14F, FontStyle.Bold),
				ForeColor = Color.White,
				Location = new Point(120, 140),
				Size = new Size(300, 40),
				TextAlign = ContentAlignment.MiddleCenter,
				BackColor = Color.Transparent
			};
			videoPanel.Controls.Add(lblLoading);

			try
			{
				string videoPath = Path.Combine(Directory.GetCurrentDirectory(), "tonytonychopper.mp4");
				
				if (File.Exists(videoPath))
				{
					// Créer WebView2 silencieusement
					videoPlayer = new WebView2();
					videoPlayer.Location = new Point(0, 0);
					videoPlayer.Size = new Size(540, 304);
					
					// Initialiser WebView2
					await videoPlayer.EnsureCoreWebView2Async();
					
					// Configuration
					videoPlayer.CoreWebView2.Settings.IsScriptEnabled = true;
					videoPlayer.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
					
					// Lancer le serveur et charger la vidéo
					await TryHttpServerMethod(videoPath);
					
					// Remplacer le loading par la vidéo
					videoPanel.Controls.Clear();
					videoPanel.Controls.Add(videoPlayer);
					
					// Attendre un peu puis forcer la lecture depuis C#
					await Task.Delay(2000);
					try
					{
						await videoPlayer.ExecuteScriptAsync(@"
							var video = document.getElementById('mainVideo');
							if (video) {
								video.volume = 0.5;
								video.play();
							}
						");
					}
					catch { /* Silencieux */ }
				}
				else
				{
					lblLoading.Text = "❌ Vidéo non trouvée";
					lblLoading.ForeColor = Color.Red;
				}
			}
			catch (Exception ex)
			{
				lblLoading.Text = "❌ Erreur de chargement";
				lblLoading.ForeColor = Color.Red;
			}
		}

				private async Task TryHttpServerMethod(string videoPath)
		{
			try
			{
				// Créer un serveur HTTP local simple
				var httpListener = new System.Net.HttpListener();
				httpListener.Prefixes.Add("http://localhost:8080/");
				httpListener.Start();
				
				// Créer le HTML avec l'URL HTTP locale
				string htmlContent = $@"
					<!DOCTYPE html>
					<html>
					<head>
						<style>
							body {{ 
								margin: 0; 
								padding: 0; 
								background: transparent; 
								display: flex; 
								justify-content: center; 
								align-items: center; 
								height: 100vh; 
								overflow: hidden;
							}}
							.video-container {{
								width: 100%;
								height: 100%;
								display: flex;
								justify-content: center;
								align-items: center;
								background: transparent;
							}}
							video {{ 
								width: 100%;
								height: 100%;
								object-fit: cover;
								border-radius: 15px;
								box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
								border: 2px solid rgba(255, 255, 255, 0.1);
								background: transparent;
							}}
						</style>
					</head>
					<body>
						<div class=""video-container"">
							<video id=""mainVideo"" autoplay preload=""auto"" loop>
								<source src=""http://localhost:8080/video.mp4"" type=""video/mp4"">
								Votre navigateur ne supporte pas la lecture vidéo.
							</video>
						</div>
						<script>
							// Configuration et lancement automatique
							document.addEventListener('DOMContentLoaded', function() {{
								var video = document.getElementById('mainVideo');
								
								// Configuration du volume
								video.volume = 0.5;
								
								// Forcer la lecture avec gestion d'erreur
								video.play().then(function() {{
									console.log('Vidéo lancée avec succès');
								}}).catch(function(error) {{
									console.log('Erreur autoplay, tentative avec interaction:', error);
									// Fallback: lancer dès le premier clic
									document.addEventListener('click', function() {{
										video.play();
									}}, {{ once: true }});
								}});
								
								// S'assurer que la vidéo reste en boucle
								video.addEventListener('ended', function() {{
									video.currentTime = 0;
									video.play();
								}});
								
								// Lancer dès que les métadonnées sont chargées
								video.addEventListener('loadedmetadata', function() {{
									video.play();
								}});
								
								// Lancer dès que la vidéo peut être lue
								video.addEventListener('canplay', function() {{
									video.play();
								}});
							}});
						</script>
					</body>
					</html>";
				
				// Charger le HTML
				videoPlayer.NavigateToString(htmlContent);
				
				// Gérer les requêtes HTTP silencieusement
				_ = Task.Run(async () =>
				{
					try
					{
						while (httpListener.IsListening)
						{
							var context = await httpListener.GetContextAsync();
							var response = context.Response;
							
							if (context.Request.Url.LocalPath == "/video.mp4")
							{
								response.ContentType = "video/mp4";
								response.ContentLength64 = new FileInfo(videoPath).Length;
								response.AddHeader("Accept-Ranges", "bytes");
								
								using (var fileStream = File.OpenRead(videoPath))
								{
									await fileStream.CopyToAsync(response.OutputStream);
								}
							}
							else
							{
								response.StatusCode = 404;
							}
							
							response.Close();
						}
					}
					catch { /* Silencieux */ }
				});
			}
			catch { /* Silencieux */ }
		}

        private void btnFinish_Click(object sender, EventArgs e)
        {
            Form parentForm = this.FindForm();
			if (parentForm != null) { parentForm.Close(); }
        }
    }
}
