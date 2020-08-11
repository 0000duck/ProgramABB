using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using System.Net;
using System.IO;
using System.Runtime.InteropServices;

namespace ProgramABB
{
    public partial class MainWindow : Window
    {
        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);
        private Point GetMousePosition()
        {
            return AppWindow.PointToScreen(Mouse.GetPosition(AppWindow));
        }

        public MainWindow()
        {
            InitializeComponent();
            SynchronizeUserData();
            StartUpdateTime();
            SetControlMode(1);
            VehicleControl();

            cursorRestPosition.X = (int)this.Width / 2;
            cursorRestPosition.Y = (int)this.Height / 2;
        }

        private void ShowMyMessageBox(String message, bool beep)
        {
            if (beep)
                Console.Beep();

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                MyMessageBox_Text.Text = message;
                MyMessageBox.Visibility = Visibility.Visible;
            }));
        }

        #region Buttons
        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ButtonMinimize_Click(object sender, RoutedEventArgs e)
        {
            AppWindow.WindowState = WindowState.Minimized;
        }

        private void ButtonCameraWebPage_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://" + user.CameraIP + ":8080/");
            StopVideo();
        }

        private void ButtonRunVideo_Click(object sender, RoutedEventArgs e)
        {
            if (!isTryingToConnect)
                RunVideo();

            else ShowMyMessageBox("czekaj...", false);
        }

        private void ButtonStopVideo_Click(object sender, RoutedEventArgs e)
        {
            StopVideo();
        }

        private void UserBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            UserBasicSettings.Visibility = Visibility.Visible;
        }

        private void UserBasicSettings_MouseLeave(object sender, MouseEventArgs e)
        {
            UserBasicSettings.Visibility = Visibility.Hidden;
            EnableSettingsChange(false, false);
        }

        private void UserSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserSettingsButton.Content.ToString() == "\uE70F")
                EnableSettingsChange(true, false);

            else EnableSettingsChange(false, true);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // ????????????!!!!!!!!!!!!!?????????????????!!!!!!!!!!!??????????
        }

        private void BackToMainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            BackToMainMenu();
        }

        private void CloseMyMessageBoxButton_Click(object sender, RoutedEventArgs e)
        {
            MyMessageBox.Visibility = Visibility.Hidden;
        }

        private void SendMessageMenuButton_Click(object sender, RoutedEventArgs e)
        {
            StartMessageMode();
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void ConnectToVehicleButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectToVehicleButton.Background == Brushes.Red)
            {
                if (!isTryingToRead && !isTryingToSend)
                {
                    ConnectToVehicleButton_Text.Text = "czekaj...";
                    ConnectToVehicleButton.Background = Brushes.Green;
                }
                else ShowMyMessageBox("czekaj...", false);

                readData = true;
                sendData = true;

                Thread dataReceiver = new Thread(ReadData);
                dataReceiver.IsBackground = true;
                dataReceiver.Start();

                Thread dataSender = new Thread(SendData);
                dataSender.IsBackground = true;
                dataSender.Start();
            }
            else
            {
                SetConnectionStatus(false);
                ShowMyMessageBox("Rozłączono się z pojazdem.", false);
            }
        }

        private void VehicleControlButton_Click(object sender, RoutedEventArgs e)
        {
            MainMenu.Visibility = Visibility.Hidden;
            BackToMainMenuButton.Visibility = Visibility.Visible;
            ControlPanelMenu.Visibility = Visibility.Visible;
            SetControlMode(1);
        }

        private void ControlMode1Button_Click(object sender, RoutedEventArgs e)
        {
            SetControlMode(1);
        }

        private void ControlMode2Button_Click(object sender, RoutedEventArgs e)
        {
            SetControlMode(2);
        }

        private void ControlMode3Button_Click(object sender, RoutedEventArgs e)
        {
            SetControlMode(3);
        }

        private void ProgramVehicleButton_Click(object sender, RoutedEventArgs e)
        {
            MainMenu.Visibility = Visibility.Hidden;
            ProgramVehicleMenu.Visibility = Visibility.Visible;
            BackToMainMenuButton.Visibility = Visibility.Visible;
        }

        // BackToMainMenu mothod
        private void BackToMainMenu()
        {
            StopMessageMode();
            SendMessageMenu.Visibility = Visibility.Hidden;
            BackToMainMenuButton.Visibility = Visibility.Hidden;
            MainMenu.Visibility = Visibility.Visible;
            ControlPanelMenu.Visibility = Visibility.Hidden;
            SetControlMode(1);
            ProgramVehicleMenu.Visibility = Visibility.Hidden;
        }
        #endregion

        #region Time
        Clock clock = new Clock();

        private void StartUpdateTime()
        {
            setTime();

            DispatcherTimer timeUpdater = new DispatcherTimer();
            timeUpdater.Interval = TimeSpan.FromSeconds(1);
            timeUpdater.Tick += UpdateTime;
            timeUpdater.Start();
        }

        private void UpdateTime(Object sender, EventArgs e)
        {
            clock.AddSecond();
            clock.IncrementCounter();

            if (clock.Counter == 10800) // 10800 seconds = 3 hours
            {
                clock.SetSystemTime();
                clock.ResetCounter();
            }

            setTime();
        }

        private void setTime()
        {
            TimeHourTextBlock.Text = clock.Hour;
            TimeDayOfWeekTextBlock.Text = clock.DayOfWeek;
            TimeDateTextBlock.Text = clock.Date;
        }
        #endregion

        #region Video Stream
        private void ReceiveVideoStream()
        {
            videoRun = true;

            string videoURL = "http://" + user.CameraIP + ":8080/video";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(videoURL);
            request.Timeout = 10000;
            HttpWebResponse response;

            try
            {
                isTryingToConnect = true;

                response = (HttpWebResponse)request.GetResponse();

                Stream imageStream = response.GetResponseStream();

                const uint BufferSize = 64000;
                byte[] imageBuffer = new byte[BufferSize];
                int a = 2;
                bool startReading = false;
                byte[] startChecker = new byte[2];
                byte[] endChecker = new byte[2];

                isTryingToConnect = false;

                while (videoRun && !isTryingToConnect)
                {
                    startChecker[1] = (byte)imageStream.ReadByte();
                    endChecker[1] = startChecker[1];

                    if (startChecker[0] == 0xff && startChecker[1] == 0xd8)
                    {
                        Array.Clear(imageBuffer, 0, imageBuffer.Length);
                        imageBuffer[0] = 0xff;
                        imageBuffer[1] = 0xd8;
                        a = 2;
                        startReading = true;
                    }

                    if (endChecker[0] == 0xff && endChecker[1] == 0xd9)
                    {
                        startReading = false;
                        imageBuffer[a] = startChecker[1];
                        MemoryStream jpegStream = new MemoryStream(imageBuffer);
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = jpegStream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            VideoBox.Source = bitmap;
                        }));
                    }

                    if (startReading && a < BufferSize)
                    {
                        imageBuffer[a] = startChecker[1];
                        a++;
                    }

                    if (a == BufferSize)
                    {
                        a = 2;
                        startReading = false;
                    }

                    startChecker[0] = startChecker[1];
                    endChecker[0] = endChecker[1];
                }

                response.Close();
            }
            catch (Exception)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    StopVideo();
                }));

                ShowMyMessageBox("Nie można nawiązać połączenia z kamerą.", true);
            }

            isTryingToConnect = false;
        }

        private void RunVideo()
        {
            if (!videoRun)
            {
                ButtonRunVideo.Visibility = Visibility.Hidden;
                ButtonStopVideo.Visibility = Visibility.Visible;
                VideoBox.Visibility = Visibility.Visible;

                Thread getVideoStream = new Thread(ReceiveVideoStream);
                getVideoStream.IsBackground = true;
                getVideoStream.Start();
            }
        }

        private void StopVideo()
        {
            videoRun = false;

            ButtonRunVideo.Visibility = Visibility.Visible;
            ButtonStopVideo.Visibility = Visibility.Hidden;
            VideoBox.Visibility = Visibility.Hidden;
        }

        private bool videoRun = false;
        private bool isTryingToConnect = false;
        #endregion

        #region User
        User user = new User();

        void SynchronizeUserData()
        {
            UserNickBlock.Text = user.Nick;
            EmailAddressBox.Text = user.Email;
            VehicleIPAddressBox.Text = user.VehicleIP;
            CameraIPAddressBox.Text = user.CameraIP;
        }

        void EnableSettingsChange(bool really, bool saveChanges)
        {
            if (really)
            {
                UserSettingsButton.Content = "\uE73E";
                UserSettingsButton.Background = Brushes.Green;
            }
            else
            {
                UserSettingsButton.Content = "\uE70F";
                UserSettingsButton.Background = Brushes.Transparent;
            }

            EmailAddressBox.IsEnabled = really;
            VehicleIPAddressBox.IsEnabled = really;
            CameraIPAddressBox.IsEnabled = really;

            if (saveChanges)
            {
                user.Email = EmailAddressBox.Text;
                user.VehicleIP = VehicleIPAddressBox.Text;
                user.CameraIP = CameraIPAddressBox.Text;

                ShowMyMessageBox("Zmiany zostały zapisane.", false);
            }
            else SynchronizeUserData();
        }
        #endregion

        #region E-Mail Message
        EmailMessage message = null;

        private void StartMessageMode()
        {
            message = new EmailMessage();

            MainMenu.Visibility = Visibility.Hidden;
            BackToMainMenuButton.Visibility = Visibility.Visible;
            SendMessageMenu.Visibility = Visibility.Visible;
        }

        private void StopMessageMode()
        {
            if (message != null)
                message = null;

            MessageClearTextBoxes();
        }

        private void SendMessage()
        {
            if (MessageBodyTextBox.Text == "")
            {
                ShowMyMessageBox("Wiadomość nie może być pusta.", true);
                return;
            }

            message.SetSubject(MessageSubjectTextBox.Text + " -> " + user.Email);
            message.SetBody(MessageBodyTextBox.Text);

            if (!message.Send())
                ShowMyMessageBox("Nie udało się wysłać wiadomości.", true);

            else ShowMyMessageBox("Wiadomość została wysłana.", false);

            StopMessageMode();
        }

        private void MessageClearTextBoxes()
        {
            MessageSubjectTextBox.Text = "";
            MessageBodyTextBox.Text = "";
        }
        #endregion

        #region Connection
        private Connection connection = new Connection();

        private void ReadData()
        {
            while (readData && !isTryingToRead)
            {
                isTryingToRead = true;

                if (connection.Read(user.VehicleIP))
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        BatteryUpdate();
                        SetConnectionStatus(true);
                    }));
                }
                else
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        SetConnectionStatus(false);
                        ShowMyMessageBox("Problem z odbieraniem danych.", true);
                    }));
                }

                isTryingToRead = false;

                Thread.Sleep(2000);
            }

            isTryingToRead = false;
        }

        private void SendData(Object stateInfo)
        {
            while (sendData && !isTryingToSend)
            {
                isTryingToSend = true;

                if (connection.Send(user.VehicleIP, data1 + data2 + data3))
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        SetConnectionStatus(true);
                    }));
                }
                else
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        SetConnectionStatus(false);
                        ShowMyMessageBox("Problem z wysłaniem danych.", true);
                    }));
                }

                isTryingToSend = false;

                if (controlMode == 3)
                {
                    //ShowMyMessageBox(mouseWheelDelta.ToString(), false);
                    mouseWheelDelta = 0;                    
                }

                Thread.Sleep(sendTimeIntervalAsMillis);
            }

            isTryingToSend = false;
        }

        private void SetConnectionStatus(bool connected)
        {
            if (connected)
            {
                ConnectToVehicleButton_Text.Text = "Połączono";
                ConnectionStatus.Foreground = Brushes.LightGreen;
            }
            else
            {
                readData = false;
                sendData = false;

                ConnectionStatus.Foreground = Brushes.White;
                ConnectToVehicleButton.Background = Brushes.Red;
                ConnectToVehicleButton_Text.Text = "Brak połączenia";
            }
        }

        private void BatteryUpdate()
        {
            BatteryBox.Text = connection.ReceivedData + "%";

            int battery = Int32.Parse(connection.ReceivedData);
            bool critical = false;

            if (battery >= 90)
                BatteryStatus.Text = "\uEBAA";
            else if (battery >= 80)
                BatteryStatus.Text = "\uEBA9";
            else if (battery >= 70)
                BatteryStatus.Text = "\uEBA8";
            else if (battery >= 60)
                BatteryStatus.Text = "\uEBA7";
            else if (battery >= 50)
                BatteryStatus.Text = "\uEBA6";
            else if (battery >= 40)
                BatteryStatus.Text = "\uEBA5";
            else if (battery >= 30)
                BatteryStatus.Text = "\uEBA4";
            else if (battery >= 20)
                BatteryStatus.Text = "\uEBA3";
            else if (battery >= 10)
            {
                BatteryStatus.Text = "\uEBA2";
                critical = true;
            }
            else if (battery >= 5)
            {
                BatteryStatus.Text = "\uEBA1";
                critical = true;
            }
            else
            {
                BatteryStatus.Text = "\uEBA0";
                critical = true;
            }

            if (critical) BatteryStatus.Foreground = Brushes.Red;
            else BatteryStatus.Foreground = Brushes.White;
        }

        private bool readData = false;
        private bool sendData = false;
        private bool isTryingToRead = false;
        private bool isTryingToSend = false;
        #endregion

        #region Control
        private void AppWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && controlMode != 1)
                SetControlMode(1);

            if (ControlPanelMenu.Visibility == Visibility.Visible && UserBasicSettings.Visibility == Visibility.Hidden)
            {
                if (e.Key == Key.D1) SetControlMode(1);
                if (e.Key == Key.D2) SetControlMode(2);
                if (e.Key == Key.D3) SetControlMode(3);

                if (controlMode == 1)
                {
                    if (e.Key == Key.W) ArrowUp.Opacity = 0.3;
                    if (e.Key == Key.S) ArrowDown.Opacity = 0.3;
                    if (e.Key == Key.A) ArrowLeft.Opacity = 0.3;
                    if (e.Key == Key.D) ArrowRight.Opacity = 0.3;
                }
                else if (controlMode == 2)
                {
                    if (e.Key == Key.Space) SpaceStopButton.Opacity = 0.3;
                    if (e.Key == Key.W)
                    {
                        if (isSpeedLocked) LockSpeed(false);
                        else LockSpeed(true);
                    }
                }
                else if (controlMode == 3)
                {
                    if (e.Key == Key.W) ArrowUpMode3.Opacity = 0.3;
                    if (e.Key == Key.S) ArrowDownMode3.Opacity = 0.3;
                    if (e.Key == Key.A) ArrowLeftMode3.Opacity = 0.3;
                    if (e.Key == Key.D) ArrowRightMode3.Opacity = 0.3;

                    if (e.Key == Key.Space)
                    {
                        if (!isArmClenched)
                        {
                            ButtonClench.Opacity = 0.3;
                            isArmClenched = true;
                        }
                        else
                        {
                            ButtonClench.Opacity = 1;
                            isArmClenched = false;
                        }
                    }
                }
            }
        }

        private void AppWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.W) ArrowUp.Opacity = 1;
            if (e.Key == Key.S) ArrowDown.Opacity = 1;
            if (e.Key == Key.A) ArrowLeft.Opacity = 1;
            if (e.Key == Key.D) ArrowRight.Opacity = 1;
            if (e.Key == Key.Space) SpaceStopButton.Opacity = 1;

            if (e.Key == Key.W) ArrowUpMode3.Opacity = 1;
            if (e.Key == Key.S) ArrowDownMode3.Opacity = 1;
            if (e.Key == Key.A) ArrowLeftMode3.Opacity = 1;
            if (e.Key == Key.D) ArrowRightMode3.Opacity = 1;
        }

        private void AppWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //ShowMyMessageBox("asdfasdfasdf", false);
        }

        private void AppWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (controlMode == 2)
            {
                if (GetMousePosition() == cursorRestPosition || sliderWait)
                {
                    sliderWait = false;
                    SetCursorPos((int)cursorRestPosition.X, (int)cursorRestPosition.Y);
                    return;
                }

                SliderX.Value += (GetMousePosition().X - cursorRestPosition.X) / 40;
                if (!isSpeedLocked) SliderY.Value -= (GetMousePosition().Y - cursorRestPosition.Y) / 40;
            }

            if (controlMode == 3)
            {
                if (GetMousePosition() == cursorRestPosition)
                    return;

                if (!mode3Wait)
                {
                    SliderXMode3.Value += GetMousePosition().X - cursorRestPosition.X;
                    SliderYMode3.Value -= GetMousePosition().Y - cursorRestPosition.Y;
                }

                mode3Wait = false;

                //MouseMode3X.Text = (GetMousePosition().X - cursorRestPosition.X).ToString();

                //if (MouseMode3X.Text.Length < 4)
                //{
                //    if (MouseMode3X.Text.Length == 3) MouseMode3X.Text = " " + MouseMode3X.Text;
                //    if (MouseMode3X.Text.Length == 2) MouseMode3X.Text = "  " + MouseMode3X.Text;
                //    if (MouseMode3X.Text.Length == 1) MouseMode3X.Text = "   " + MouseMode3X.Text;
                //}
            }

            if (controlMode == 2 || controlMode == 3)
                SetCursorPos((int)cursorRestPosition.X, (int)cursorRestPosition.Y);
        }

        private void AppWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (controlMode == 3)
            {
                if (e.Delta > 0)
                {
                    mouseWheelDelta++;
                    MouseWheelArrow.Text = "\uE96D";
                }
                else if (e.Delta < 0)
                {
                    mouseWheelDelta--;
                    MouseWheelArrow.Text = "\uE96E";
                }

                if (mouseWheelDelta > 9) mouseWheelDelta = 9;
                if (mouseWheelDelta < -9) mouseWheelDelta = -9;

                mouseWheelWait = true;
            }
        }

        private void VehicleControl()
        {
            DispatcherTimer keyboardSupport = new DispatcherTimer();
            keyboardSupport.Interval = TimeSpan.FromMilliseconds(1);
            keyboardSupport.Tick += ControlVehicle;
            keyboardSupport.Start();
        }

        private void ControlVehicle(Object sender, EventArgs e)
        {
            data2 = "";
            data3 = "";

            //if (ProgramVehicleStackPanel.Children.Count > 0)//////////////////
                //ShowMyMessageBox(steeringBoxList[0].IsChecked.ToString(), false);

            switch (controlMode)
            {
                case 1:

                    if (Keyboard.IsKeyDown(Key.W) && System.Windows.Input.Keyboard.IsKeyDown(Key.S))
                        data2 = "49";
                    else if (Keyboard.IsKeyDown(Key.S))
                        data2 = "00";
                    else if (Keyboard.IsKeyDown(Key.W))
                        data2 = "98";
                    else data2 = "49";

                    if (Keyboard.IsKeyDown(Key.D) && Keyboard.IsKeyDown(Key.A))
                        data2 += "49";
                    else if (Keyboard.IsKeyDown(Key.A))
                        data2 += "00";
                    else if (Keyboard.IsKeyDown(Key.D))
                        data2 += "98";
                    else data2 += "49";

                    break;

                case 2:

                    int x = (int)SliderX.Value;
                    int y = (int)SliderY.Value;

                    String xAsString = x.ToString();
                    String yAsString = y.ToString();

                    if (x < 10) xAsString = "0" + x.ToString();
                    if (y < 10) yAsString = "0" + y.ToString();

                    data2 = yAsString;
                    data2 += xAsString;

                    if (Keyboard.IsKeyDown(Key.Space))
                        SetControlMode(2);

                    break;

                case 3:

                    if (mouseWheelWait)
                    {
                        mouseWheelWait = false;
                    }
                    else
                    {
                        MouseWheelArrow.Text = "";
                    }

                    int qx = (int)SliderXMode3.Value;
                    int qy = (int)SliderYMode3.Value;
                    qx -= 49;
                    qy -= 49;
                    qx /= 2;
                    qy /= 2;
                    qx += 49;
                    qy += 49;
                    SliderXMode3.Value = qx;
                    SliderYMode3.Value = qy;
                    if (SliderXMode3.Value < 51 && SliderXMode3.Value > 47) SliderXMode3.Value = 49;
                    if (SliderYMode3.Value < 51 && SliderYMode3.Value > 47) SliderYMode3.Value = 49;

                    String x3AsString = SliderXMode3.Value.ToString();
                    String y3AsString = SliderYMode3.Value.ToString();

                    if (SliderXMode3.Value < 10) x3AsString = "0" + SliderXMode3.Value.ToString();
                    if (SliderXMode3.Value < 10) y3AsString = "0" + SliderYMode3.Value.ToString();

                    data2 = y3AsString;
                    data2 += x3AsString;

                    int wheel = mouseWheelDelta + 9;
                    String mouseWheelDeltaAsString = wheel.ToString();

                    if (wheel < 10) mouseWheelDeltaAsString = "0" + wheel.ToString();

                    data2 += mouseWheelDeltaAsString;

                    if (Keyboard.IsKeyDown(Key.W) && System.Windows.Input.Keyboard.IsKeyDown(Key.S))
                        data2 += "0";
                    else if (Keyboard.IsKeyDown(Key.S))
                        data2 += "1";
                    else if (Keyboard.IsKeyDown(Key.W))
                        data2 += "2";
                    else data2 += "0";

                    if (Keyboard.IsKeyDown(Key.D) && Keyboard.IsKeyDown(Key.A))
                        data2 += "0";
                    else if (Keyboard.IsKeyDown(Key.A))
                        data2 += "1";
                    else if (Keyboard.IsKeyDown(Key.D))
                        data2 += "2";
                    else data2 += "0";

                    data2 += isArmClenched ? "1" : "0";

                    break;
            }
        }

        private void SetControlMode(int m)
        {
            if (m == 1 && controlMode != m)
                SetCursorPos((int)cursorRestPosition.X, (int)cursorRestPosition.Y);

            controlMode = m;

            if (m == 2 || m == 3)
            {
                SetCursorPos((int)this.Width / 2, (int)this.Height / 2);
                AppMainGrid.IsEnabled = false;
                Cursor = Cursors.None;
            }
            else
            {
                AppMainGrid.IsEnabled = true;
                Cursor = Cursors.Arrow;
            }

            Thickness margin = ControlMode1Button.Margin;
            margin.Left = 0;
            margin.Right = 0;
            margin.Top = 0;
            margin.Bottom = 0;

            Thickness margin2 = ControlMode1Button.Margin;
            margin2.Left = 5;
            margin2.Right = 5;
            margin2.Top = 5;
            margin2.Bottom = 30;

            switch (m)
            {
                case 1:
                    data1 = "1";

                    ControlMode1Button.Margin = margin;
                    ControlMode2Button.Margin = margin2;
                    ControlMode3Button.Margin = margin2;

                    ControlMode1Grid.Visibility = Visibility.Visible;
                    ControlMode2Grid.Visibility = Visibility.Hidden;
                    ControlMode3Grid.Visibility = Visibility.Hidden;

                    break;

                case 2:
                    data1 = "1";

                    SliderX.Value = 49;
                    SliderY.Value = 49;

                    sliderWait = true;
                    LockSpeed(false);

                    ControlMode1Button.Margin = margin2;
                    ControlMode2Button.Margin = margin;
                    ControlMode3Button.Margin = margin2;

                    ControlMode1Grid.Visibility = Visibility.Hidden;
                    ControlMode2Grid.Visibility = Visibility.Visible;
                    ControlMode3Grid.Visibility = Visibility.Hidden;
                    break;

                case 3:
                    data1 = "2";

                    SliderXMode3.Value = 49;
                    SliderYMode3.Value = 49;

                    mode3Wait = true;

                    ControlMode1Button.Margin = margin2;
                    ControlMode2Button.Margin = margin2;
                    ControlMode3Button.Margin = margin;

                    ControlMode1Grid.Visibility = Visibility.Hidden;
                    ControlMode2Grid.Visibility = Visibility.Hidden;
                    ControlMode3Grid.Visibility = Visibility.Visible;
                    break;
            }

            data2 = "";
            data3 = "";
        }

        private void LockSpeed(bool x)
        {
            if (x)
            {
                isSpeedLocked = true;
                SpeedLockButton.Opacity = 0.3;
            }
            else
            {
                isSpeedLocked = false;
                SpeedLockButton.Opacity = 1;
            }
        }

        private int controlMode = 1;
        private Point cursorRestPosition;
        private String data1;
        private String data2;
        private String data3;
        private int mouseWheelDelta = 0;
        private bool mouseWheelWait = false;
        private bool sliderWait = false;
        private bool isSpeedLocked = false;
        private bool mode3Wait = false;
        private bool isArmClenched = false;
        #endregion

        private void NewSteeringBoxVehicleButton_Click(object sender, RoutedEventArgs e)
        {
            AddSteeringBox();
        }

        private void DeleteLastSteeringBoxButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveLastSteeringBox(ProgramVehicleStackPanel);
        }

        private void DeleteSelectedBoxesButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProgramVehicleStackPanel.Children.Count > 0)
            {
                for (int i = ProgramVehicleStackPanel.Children.Count - 1; i >= 0; i--)
                {
                    if (steeringBoxList[i].IsChecked)
                        RemoveSteeringBox(i, ProgramVehicleStackPanel);
                }
            }
        }

        private void DeleteAllSteeringBoxesButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveAllSteeringBoxes(ProgramVehicleStackPanel);
        }

        private void AddSteeringBox()
        {
            SteeringBox steeringBox = new SteeringBox(ProgramVehicleStackPanel, sendTimeIntervalAsMillis);
            steeringBoxList.Add(steeringBox);
        }

        private void AddSteeringBox(int index)
        {
            SteeringBox steeringBox = new SteeringBox(ProgramVehicleStackPanel, sendTimeIntervalAsMillis, index);

            int x = steeringBoxList.Count - 1;

            if (index < 0) steeringBoxList.Insert(0, steeringBox);
            else if (index > x) steeringBoxList.Add(steeringBox);
            else steeringBoxList.Insert(index, steeringBox);
        }

        private void RemoveLastSteeringBox(StackPanel panel)
        {
            if (panel.Children.Count > 0)
            {
                int x = panel.Children.Count - 1;
                panel.Children.RemoveAt(x);
                steeringBoxList.RemoveAt(x);
            }
        }

        private void RemoveSteeringBox(int index, StackPanel panel)
        {
            if (panel.Children.Count > 0)
            {
                int x = panel.Children.Count - 1;

                if (index >= 0 && index <= x)
                {
                    panel.Children.RemoveAt(index);
                    steeringBoxList.RemoveAt(index);
                }
            }
        }

        private void RemoveAllSteeringBoxes(StackPanel panel)
        {
            panel.Children.Clear();
            steeringBoxList.Clear();
        }

        private IList<SteeringBox> steeringBoxList = new List<SteeringBox>();

        private const int sendTimeIntervalAsMillis = 50;
    }

    class SteeringBox
    {
        public SteeringBox(StackPanel panel, int timeInterval, int index = -1)
        {
            Border border = new Border()
            {
                Height = 90,
                BorderThickness = new Thickness(4),
                BorderBrush = new SolidColorBrush(Colors.Yellow)
            };

            Grid mainGrid = new Grid() { Background = Brushes.White };
            border.Child = mainGrid;

            // Row definition
            RowDefinition mainGridRow1 = new RowDefinition();
            RowDefinition mainGridRow2 = new RowDefinition();
            RowDefinition mainGridRow3 = new RowDefinition();
            mainGrid.RowDefinitions.Add(mainGridRow1);
            mainGrid.RowDefinitions.Add(mainGridRow2);
            mainGrid.RowDefinitions.Add(mainGridRow3);
            mainGridRow1.Height = new GridLength(1, GridUnitType.Star);
            mainGridRow2.Height = new GridLength(1, GridUnitType.Star);
            mainGridRow3.Height = new GridLength(1, GridUnitType.Star);

            // Column definition
            ColumnDefinition mainGridCol1 = new ColumnDefinition();
            ColumnDefinition mainGridCol2 = new ColumnDefinition();
            ColumnDefinition mainGridCol3 = new ColumnDefinition();
            mainGrid.ColumnDefinitions.Add(mainGridCol1);
            mainGrid.ColumnDefinitions.Add(mainGridCol2);
            mainGrid.ColumnDefinitions.Add(mainGridCol3);
            mainGridCol1.Width = new GridLength(50);
            mainGridCol2.Width = new GridLength(1, GridUnitType.Star);
            mainGridCol3.Width = new GridLength(1, GridUnitType.Auto);

            // Speed
            // Speed slider
            Slider speedSlider = new Slider()
            {
                Maximum = 100,
                Minimum = -100,
                Value = 0,
                IsSnapToTickEnabled = true,
                TickFrequency = 1,
                IsTabStop = false,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(speedSlider, 1);
            mainGrid.Children.Add(speedSlider);
            // Speed textBox
            TextBox speedTextBox = new TextBox()
            {
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            mainGrid.Children.Add(speedTextBox);
            // Binding
            speedTextBox.SetBinding(TextBox.TextProperty, new Binding("Value") { Source = speedSlider });
            speedSlider.SetBinding(Slider.ValueProperty, new Binding("Speed") { Source = this });

            // Turn
            // Turn slider
            Slider turnSlider = new Slider()
            {
                Maximum = 100,
                Minimum = -100,
                Value = 0,
                IsSnapToTickEnabled = true,
                TickFrequency = 1,
                IsTabStop = false,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(turnSlider, 1);
            Grid.SetRow(turnSlider, 1);
            mainGrid.Children.Add(turnSlider);
            // Turn textBox
            TextBox turnTextBox = new TextBox()
            {
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(turnTextBox, 1);
            mainGrid.Children.Add(turnTextBox);
            // Binding
            turnTextBox.SetBinding(TextBox.TextProperty, new Binding("Value") { Source = turnSlider });
            turnSlider.SetBinding(Slider.ValueProperty, new Binding("Turn") { Source = this });

            // Time
            // Time slider
            Slider timeSlider = new Slider()
            {
                Maximum = timeInterval * 200,
                Minimum = 0,
                Value = 0,
                IsSnapToTickEnabled = true,
                TickFrequency = timeInterval,
                IsTabStop = false,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(timeSlider, 1);
            Grid.SetRow(timeSlider, 2);
            mainGrid.Children.Add(timeSlider);
            // Turn textBox
            TextBox timeTextBox = new TextBox()
            {
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                IsEnabled = false
            };
            Grid.SetRow(timeTextBox, 2);
            mainGrid.Children.Add(timeTextBox);
            // Binding
            timeTextBox.SetBinding(TextBox.TextProperty, new Binding("Value") { Source = timeSlider });
            timeSlider.SetBinding(Slider.ValueProperty, new Binding("TimeAsMillis") { Source = this });

            // CheckBox
            // Background
            Rectangle rect = new Rectangle() { Fill = Brushes.LightGray };
            Grid.SetColumn(rect, 2);
            Grid.SetRowSpan(rect, 3);
            mainGrid.Children.Add(rect);
            // Box
            CheckBox checkBox = new CheckBox()
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                IsTabStop = false
            };
            Grid.SetColumn(checkBox, 2);
            Grid.SetRowSpan(checkBox, 3);
            mainGrid.Children.Add(checkBox);
            // Binding
            checkBox.SetBinding(RadioButton.IsCheckedProperty, new Binding("IsChecked") { Source = this });

            int x = panel.Children.Count - 1;

            if (index < 0 || index > x)
                panel.Children.Add(border);

            else panel.Children.Insert(index, border);
        }

        public bool Mode { get; private set; }
        public bool IsChecked { get; set; }

        // Mode 1
        public int TimeAsMillis { get; set; }
        public int Speed { get; set; }
        public int Turn { get; set; }

        // Mode 2
    }
}
