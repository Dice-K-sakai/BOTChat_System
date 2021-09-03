using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ChatSystem;

namespace ChatSystem
{
    class main
    {
        static ChatSystem chatSystem;
        const Int32 portNo = 11000;
        const string EOF = "<EOF>";
        static readonly int maxLength = 200 + EOF.Length;
        static ChatSystem.ConnectMode connectMode;

        static string received = "";

        static bool isBot = false;
        static Random rand = new Random();

        static string user_name = "";

        static void Main(string[] args)
        {

            chatSystem = new ChatSystem(maxLength);
            Console.WriteLine($"このPCのホスト名は {chatSystem.hostName}です。");
            connectMode = SelectMode();

            while (true)
            {
                if (connectMode != ChatSystem.ConnectMode.host && !isBot)
                {
                    Console.Write("名前を入力してください。:");
                    user_name = Console.ReadLine();

                    if (user_name != "")
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine("読み取りに失敗しました、もう一度入力してしてください。\n");
                    }
                }
                else
                {
                    user_name = "Bot_Host";
                    break;
                }

            }

            InChat();

        }

        static ChatSystem.ConnectMode SelectMode()
        {
            ChatSystem.ConnectMode connectMode = ChatSystem.ConnectMode.host;

            while (true)
            {
                Console.Write("モードを選択してください。[ 0か1を入力 ]\n{ 0 : Host , 1 : Client } : ");
                int select = int.Parse(Console.ReadLine());

                switch (select)
                {
                    case 0:

                        //Host
                        Console.WriteLine("ホストモードで起動します。");

                        isBot = SelectMode_Bot();
                        InitializeHost();
                        connectMode = ChatSystem.ConnectMode.host;
                        break;

                    case 1:

                        //Client
                        Console.WriteLine("クライアントモードで起動します。");
                        InitializeClient();
                        connectMode = ChatSystem.ConnectMode.client;
                        break;

                    default:

                        Console.WriteLine("入力が未定義でした。もう一度入力してください。\n");
                        break;
                }

                if (select == 0 || select == 1)
                {
                    break;
                }
            }

            return connectMode;
        }

        static bool SelectMode_Bot()
        {
            bool isSelect = false;

            //ボットモード [ ON / OFF ]
            while (true)
            {
                Console.Write("ホストをbotにしますか? [ 0か1を入力 ]\n{ 0 : No , 1 : Yes } :");
                int select = int.Parse(Console.ReadLine());

                switch (select)
                {
                    case 0:

                        //No
                        Console.WriteLine("通常モードで起動します。\n");
                        isSelect = false;
                        break;

                    case 1:

                        //Yes
                        Console.WriteLine("ボットモードで起動します。\n");
                        isSelect = true;
                        break;

                    default:

                        Console.WriteLine("入力が未定義でした。もう一度入力してください。\n");
                        break;
                }

                if (select == 0 || select == 1)
                {
                    break;
                }
            }

            return isSelect;

        }

        static void InitializeHost()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(chatSystem.hostName);

            foreach (var addresslist in ipHostInfo.AddressList)
            {
                Console.WriteLine($"自分のアドレスが見つかりました:{addresslist.ToString()}");
            }

            IPAddress ipAddress = ipHostInfo.AddressList[ipHostInfo.AddressList.Length - 1];

            if (!isBot)
            {
                Console.Write($"\n公開するアドレスを選択してください。(0 から {ipHostInfo.AddressList.Length - 1}):");
                ipAddress = ipHostInfo.AddressList[int.Parse(Console.ReadLine())];
            }

            ChatSystem.EResult re = chatSystem.InitializeHost(ipAddress, portNo);

            Console.WriteLine("\n\n\n");//改行

            if (re != ChatSystem.EResult.success)
            {
                Console.WriteLine($"初期化に失敗しました。\nエラー内容 = {re.ToString()}");
            }

        }

        static void InitializeClient()
        {
            Console.Write("接続するIPアドレスを入力してください。:");
            var ipAddress = IPAddress.Parse(Console.ReadLine());
            ChatSystem.EResult re = chatSystem.InitializeClient(ipAddress, 11000);

            if (re == ChatSystem.EResult.success)
            {
                Console.WriteLine($"接続されたホスト。:{ipAddress.ToString()} \n\n\n");
            }
            else
            {
                Console.WriteLine($"ホストへの接続に失敗しました。\nエラー内容 ={chatSystem.resultMessage}");
            }
        }

        static void InChat()
        {
            Console.WriteLine("チャット開始");

            ChatSystem.Buffer buffer = new ChatSystem.Buffer(maxLength);
            bool turn = (connectMode == ChatSystem.ConnectMode.host);

            while (true)
            {
                if (turn)
                {
                    // 受信
                    buffer = new ChatSystem.Buffer(maxLength);
                    ChatSystem.EResult re = chatSystem.Receive(buffer);

                    if (re == ChatSystem.EResult.success)
                    {
                        received = Encoding.UTF8.GetString(buffer.content).Replace(EOF, "");
                        int l = received.Length;

                        if (received[0] != '\0')
                        {
                            // 正常にメッセージを受信
                            Console.WriteLine($"{received}");
                        }
                        else
                        {
                            // 正常に終了を受信
                            Console.WriteLine("相手から終了を受信");
                            break;
                        }
                    }
                    else
                    {
                        //　受信エラー
                        Console.WriteLine($"受信エラー：{chatSystem.resultMessage} ");
                        break;
                    }
                }
                else
                {
                    // 送信
                    Console.Write(user_name + "：");
                    string inputSt = "";

                    if (isBot)
                    {
                        inputSt = Bot();
                        Console.Write(inputSt + "\n");
                    }
                    else
                    {
                        inputSt = Console.ReadLine();
                    }

                    if (inputSt.Length > maxLength)
                    {
                        inputSt = inputSt.Substring(0, maxLength - EOF.Length);
                    }

                    string refuge = user_name + " : " + inputSt + EOF;
                    inputSt = refuge;

                    buffer.content = Encoding.UTF8.GetBytes(inputSt);
                    buffer.length = buffer.content.Length;
                    ChatSystem.EResult re = chatSystem.Send(buffer);

                    if (re != ChatSystem.EResult.success)
                    {
                        Console.WriteLine($"送信エラー：{re.ToString()} エラーコード : {chatSystem.resultMessage}");
                        break;
                    }
                }

                turn = !turn;
            }

            chatSystem.ShutDownColse();
        }

        static string Bot()
        {
            string reply = "…。";
            int isOne = 0;

            if (received.Contains("こんにちは") || received.Contains("こんちわ") || received.Contains("こん"))
            {
                reply = "こんにちは。";
            }

            if (received.Contains("ですか?") || received.Contains("ですか？"))
            {
                isOne++;

                if (rand.Next(0, 10) < 5)
                {
                    reply = "はい。\n";
                }
                else
                {
                    reply = "いいえ。\n";
                }
            }

            if (received.Contains("すき") || received.Contains("好き") || received.Contains("スキ"))
            {
                isOne++;

                if (rand.Next(0, 10) < 5)
                {
                    reply = "私も好きです。\n";
                }
                else
                {
                    reply = "私は嫌いです。\n";
                }
            }

            if (isOne >= 1)
            {
                reply = "出来れば、一つずつ質問してもらうと良いかなって私思って…。\n";
            }

            return reply;
        }
    }
}