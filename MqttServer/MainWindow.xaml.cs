using MQTTnet;
using MQTTnet.Server;
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

namespace MqttServer
{
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IMqttServer server;
        List<UserInstance> instances;

        public MainWindow()
        {
            InitializeComponent();
            instances = new List<UserInstance>();
        }
        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            var optionBuilder = new MqttServerOptionsBuilder().
                WithDefaultEndpoint().WithDefaultEndpointPort(1883).WithConnectionValidator(
                c =>
                {
                    var flag = (c.Username != "" && c.Password != "") ? true : false;
                    if (!flag)
                    {
                        c.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.BadUserNameOrPassword;
                        return;
                    }
                    c.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.Success;
                    instances.Add(new UserInstance()
                    {
                        clientId = c.ClientId,
                        userName = c.Username,
                        passWord = c.Password
                    });
                    Showlog($"{DateTime.Now}:账号:{c.Username}已订阅!\r\n");
                }).WithSubscriptionInterceptor(c =>
                {
                    if (c == null) return;
                    c.AcceptSubscription = true;
                    Showlog($"{DateTime.Now}:订阅者{c.ClientId}\r\n");
                }).WithApplicationMessageInterceptor(c =>
                {
                    if (c == null) return;
                    c.AcceptPublish = true;
                    string str = c.ApplicationMessage?.Payload == null ? null : Encoding.UTF8.GetString(c.ApplicationMessage?.Payload) + "\r\n";
                    Showlog($"{DateTime.Now}:{str}\r\n");
                })
                ;

            server = new MqttFactory().CreateMqttServer();
            server.UseClientDisconnectedHandler(c =>
            {
                var use = instances.FirstOrDefault(t => t.clientId == c.ClientId);
                if (use != null)
                {
                    instances.Remove(use);
                    Showlog($"{DateTime.Now}:订阅者{use.userName}已退出\r\n");
                }
            });
            await server.StartAsync(optionBuilder.Build());
        }

        private void Showlog(string message)
        {
            this.Dispatcher.Invoke(() =>
            {
                txtresult.Text += message;
            });
        }

        private void Broadcast_Click(object sender, RoutedEventArgs e)
        {
            instances.ForEach(arg =>
            {
                server.PublishAsync(new MqttApplicationMessage()
                {
                    Topic = arg.clientId,
                    QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce,
                    Retain = false,
                    Payload = Encoding.UTF8.GetBytes($"{DateTime.Now}:服务器:明天都不要来上班了！")
                }); ;
            });
        }
    }
}