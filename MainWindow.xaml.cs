using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.IO;
using System;
using System.Threading.Tasks;

namespace KrestikiNoliki
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TcpClient client;
        private NetworkStream stream;
        private Button[] boardButtons = new Button[9];

        public MainWindow()
        {
            InitializeComponent();
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            for (int i = 0; i < 9; i++)
            {
                Button button = new Button { Tag = i, FontSize = 24 };
                button.Click += BoardButton_Click;
                BoardGrid.Children.Add(button);
                boardButtons[i] = button;
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string serverIp = ServerIpTextBox.Text;
                int port = int.Parse(PortTextBox.Text);
                string playerName = NameTextBox.Text;

                client = new TcpClient(serverIp, port);
                stream = client.GetStream();

                byte[] nameData = Encoding.UTF8.GetBytes(playerName);
                await stream.WriteAsync(nameData, 0, nameData.Length);

                await Task.Run(() => ReceiveGameState());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к серверу: {ex.Message}");
            }
        }

        private async void BoardButton_Click(object sender, RoutedEventArgs e)
        {
            if (client == null || !client.Connected)
            {
                MessageBox.Show("Нет подключения к серверу.");
                return;
            }

            Button button = sender as Button;
            int position = (int)button.Tag;

            byte[] moveData = Encoding.UTF8.GetBytes(position.ToString());
            await stream.WriteAsync(moveData, 0, moveData.Length);
        }

        private void ReceiveGameState()
        {
            byte[] buffer = new byte[256];
            while (true)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        MessageBox.Show("Соединение с сервером закрыто.");
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    if (message.StartsWith("RESULT|"))
                    {
                        string resultMessage = message.Substring(7);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(resultMessage);
                        });
                        break;
                    }

                    string[] parts = message.Split('|');
                    string[] boardState = parts[0].Split(',');
                    string player1 = parts[1];
                    string player2 = parts[2];
                    int currentPlayer = int.Parse(parts[3]);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        for (int i = 0; i < boardState.Length; i++)
                        {
                            boardButtons[i].Content = boardState[i] == " " ? "" : boardState[i];
                        }

                        Title = $"Крестики-нолики - {player1} против {player2} - Ход {(currentPlayer == 0 ? "X" : "O")}";
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при получении состояния игры: {ex.Message}");
                    break;
                }
            }
        }
    }
}