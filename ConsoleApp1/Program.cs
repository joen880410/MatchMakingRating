using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading.Tasks;
using System.Timers;

namespace MMRMatchmaking
{

    // 玩家類別
    class Player
    {
        public string Name { get; set; }
        public double MMR { get; set; }
        public DateTime JoinInQueueTime { get; set; }
        public double Distance { get; set; }
        private double _speed;
        public double Speed
        {
            get
            {
                if (_speed == 0)
                {
                    foreach (var item in SpeedMappingDic)
                    {
                        if (MMR <= item.Key)
                        {
                            _speed = item.Value;
                            break;
                        }
                        _speed = SpeedMappingDic.Last().Value;
                    }
                }
                return _speed;
            }
            set
            {
                foreach (var item in SpeedMappingDic)
                {
                    if (MMR <= item.Key)
                    {
                        _speed = item.Value;
                        return;
                    }
                    _speed = SpeedMappingDic.Last().Value;
                }
            }
        }
        public bool IsInQueue { get; set; }
        public bool IsEndGame { get; set; } = false;

        private readonly Dictionary<int, double> SpeedMappingDic = new Dictionary<int, double>()
        {
            { 1000,2.2 },
            { 1020,2.5 },
            { 1040,2.8 },
            { 1060,3.1 },
        };

        public Player(string name, double mmr)
        {
            Name = name;
            MMR = mmr;
        }

        public override string ToString()
        {
            return $"{Name}(MMR: {(int)MMR})";
        }
    }

    class Program
    {
        private const int TotalPlayerCount = 20;
        private const int OneTeamPlayerCount = 8;
        private const int MinThreshold = 10;
        private const int MaxThreshold = 60;
        private const int TotalDistance = 10;

        private readonly static Random random = new Random();

        private readonly static List<Player> totalPlayers = new List<Player>(TotalPlayerCount);
        private readonly static List<Player> pairTeams = new List<Player>(OneTeamPlayerCount);
        private static int threshold = MinThreshold;
        private static Timer _timer;
        private static DateTime lastJoinPlayerTime = DateTime.Now;
        private static DateTime lastPlayerTime = DateTime.Now;
        private static DateTime lastThresholdAddTime = DateTime.Now;
        private static DateTime StartGameTime = DateTime.Now;
        private static bool IsNewPairs = true;
        private static bool IsGamePlaying = false;
        private static void Main(string[] args)
        {
            while (true)
            {
                StartMatch_PlayGame(null, null);
                System.Threading.Thread.Sleep(500);
            }

            _timer = new Timer(100 * 1);
            _timer.Enabled = false;
            _timer.Elapsed += StartMatch_PlayGame;
            Console.ReadKey();

        }
        public static void StartMatch_PlayGame(object sender, ElapsedEventArgs e)
        {
            if (IsGamePlaying)
            {
                return;
            }
            if (lastJoinPlayerTime <= DateTime.Now)
            {
                JoinPlayer();
            }
            if (lastPlayerTime <= DateTime.Now)
            {
                MatchSystem();
            }
            if (!IsNewPairs && lastThresholdAddTime <= DateTime.Now && lastPlayerTime <= DateTime.Now)
            {

                lastThresholdAddTime.AddSeconds(1);
                threshold++;
                threshold = Math.Clamp(threshold, MinThreshold, MaxThreshold);
                Console.WriteLine($"跨大範圍:{threshold}");
            }
        }
        public static void JoinPlayer()
        {
            //玩家已到達指定數量
            if (totalPlayers.Count == TotalPlayerCount)
            {
                return;
            }
            // 建立一玩家，初始 MMR 隨機在 1010 ~ 1079 之間
            int initialMMR = random.Next(1010, 1080); // 1580 為上限（不包含），因此取值範圍 1010~1579
            Player player = new Player($"Player{totalPlayers.Count + 1}", initialMMR);
            player.JoinInQueueTime = DateTime.Now;
            player.IsInQueue = true;
            totalPlayers.Add(player);
            lastJoinPlayerTime = DateTime.Now.AddMilliseconds(random.Next(1, 11) * 100);
        }
        public static void MatchSystem()
        {
            if (totalPlayers.Count / OneTeamPlayerCount <= 0)
            {
                Console.WriteLine($"排隊人數不足");
                lastPlayerTime = DateTime.Now.AddMilliseconds(50);
                return;
            }

            //挑出等待時間最長的玩家或隊伍作為配對基準
            Console.WriteLine("尋找等待最久玩家");
            SelectWaitingLongerPlayer(totalPlayers);
            // 進行配對
            IsNewPairs = true;
            Console.WriteLine("開始配對");
            Matchmaking(totalPlayers, threshold);
            IsNewPairs = false;

            if (pairTeams.Count < OneTeamPlayerCount)
            {
                Console.WriteLine("配對人數不足，重新配對");
                return;
            }
            //最終檢查如果有不符合範圍就解散配對重來
            if (FinalCheck(pairTeams, MaxThreshold))
            {
                lastPlayerTime = DateTime.Now.AddMilliseconds(50);
                return;
            }

            Console.WriteLine("\n配對組合");
            foreach (var pair in pairTeams)
            {
                Console.Write($"{pair} VS ");
            }
            Console.WriteLine();
            Console.WriteLine("比賽開始");
            PlayGameSyatem();
            // 模擬每一場比賽並更新 MMR
            Console.WriteLine("\n比賽結果:");
            UpdateMMR(pairTeams);

            // 輸出比賽後玩家 MMR
            Console.WriteLine("\n比賽後玩家 MMR:");
            foreach (var p in totalPlayers)
            {
                Console.Write($"{p} - ");
            }
            Console.WriteLine();
            IsGamePlaying = false;
            //準備下一場配對
            threshold = MinThreshold;
            lastPlayerTime = DateTime.Now.AddMilliseconds(5000);
            ReEnterWaitList();
            //Console.ReadKey();
        }

        public static void PlayGameSyatem()
        {
            StartGameTime = DateTime.Now;
            while (!pairTeams.All(e => e.IsEndGame))
            {
                TimeSpan ElapsedTime = DateTime.Now - StartGameTime;
                for (int i = 0; i < pairTeams.Count; i++)
                {
                    var player = pairTeams[i];
                    var distance = ElapsedTime.TotalSeconds * player.Speed;
                    if (distance > TotalDistance)
                    {
                        Console.WriteLine($"{player.Name}到達終點");
                        player.IsEndGame = true;
                        player.Distance = distance;
                    }
                }
                System.Threading.Thread.Sleep(50);
            }
        }

        /// <summary>
        /// 根據 Elo 系統公式計算預期勝率
        /// </summary>
        static double CalculateExpectedScore(Player playerA, Player playerB)
        {
            return 1.0 / (1 + Math.Pow(10, (playerB.MMR - playerA.MMR) / 400));
        }

        /// <summary>
        /// 計算 MMR 變化，基於 Elo 但考慮排名
        /// <param name="k">變動因子</param>
        /// /// </summary>
        static void UpdateMMR(List<Player> players, double k = 40)
        {
            //根據距離排名
            players = players.OrderByDescending(e => e.Distance).ToList();
            int numPlayers = players.Count;
            double avgMMR = players.Average(p => p.MMR); // 計算平均 MMR

            for (int i = 0; i < numPlayers; i++)
            {
                Player player = players[i];
                Console.WriteLine($"第{i + 1}名:{player.Name}");

                // 依照排名計算實際得分：第一名得 1 分，最後一名得 0 分
                double score = (numPlayers - (i + 1)) / (double)(numPlayers - 1);

                // 使用 Elo 公式計算預期得分：與每個對手的一對一勝率取平均
                double expectedScore = 0.0;
                for (int j = 0; j < numPlayers; j++)
                {
                    if (i == j) continue;
                    expectedScore += CalculateExpectedScore(players[j], player);
                }
                expectedScore /= (numPlayers - 1);

                // 利用 K 值調整 MMR：實際得分與預期得分之間的差距
                double mmrChange = k * (score - expectedScore);
                player.MMR += mmrChange;
            }
        }

        /// <summary>
        /// 尋找等待最久的玩家或是隊伍
        /// </summary>
        private static void SelectWaitingLongerPlayer(List<Player> players)
        {
            pairTeams.Clear();
            // 依照 等待時間 進行排序
            var sortedPlayers = players.OrderByDescending(p => p.JoinInQueueTime).ToList();
            Console.WriteLine($"{sortedPlayers.First()}");
            //先加入第一位玩家做基準
            pairTeams.Add(sortedPlayers.First());
        }

        /// <summary>
        /// 根據玩家 MMR 配對，將 MMR 差距在指定門檻內的玩家配成一組
        /// </summary>
        private static void Matchmaking(List<Player> players, double threshold = 100)
        {
            bool[] used = new bool[players.Count];
            Player standardPlayer = pairTeams.First();
            for (int i = 1; i < players.Count; i++)
            {
                if (!players[i].IsInQueue)
                {
                    continue;
                }
                if (pairTeams.Count >= OneTeamPlayerCount)
                {
                    Console.WriteLine($"配對人數足夠，前往下一步 人數:{pairTeams.Count}");
                    break;
                }
                if (used[i])
                    continue;
                if (Math.Abs(standardPlayer.MMR - players[i].MMR) <= threshold)
                {
                    pairTeams.Add(players[i]);
                    used[i] = true;
                }
            }
        }

        /// <summary>
        /// 檢查最大MMR玩家以及最小MMR玩家差距是否在規定內
        /// </summary>
        /// <param name="startPlayGamePlayers">配對中玩家</param>
        /// <param name="MMR_Range">積分範圍</param>
        /// <returns>是否在規定內</returns>
        private static bool FinalCheck(List<Player> startPlayGamePlayers, double MMR_Range)
        {
            var MMR_MaxPlayer = startPlayGamePlayers.Max(e => e.MMR);
            var MMR_MinPlayer = startPlayGamePlayers.Min(e => e.MMR);
            var result = Math.Abs(MMR_MaxPlayer - MMR_MinPlayer) > MMR_Range;
            if (result)
            {
                Console.WriteLine($"檢查失敗，重新配對  範圍:{MMR_Range} 目前結果:{Math.Abs(MMR_MaxPlayer - MMR_MinPlayer)}");
                return true;
            }
            else
            {
                //準備比賽不隊在排隊系統裡
                for (int i = 0; i < startPlayGamePlayers.Count; i++)
                {
                    Player startPlayGamePlayer = startPlayGamePlayers[i];
                    startPlayGamePlayer.IsInQueue = false;
                    startPlayGamePlayer.IsEndGame = false;
                }
                return false;
            }
        }
        private static void ReEnterWaitList()
        {
            for (int i = 0; i < pairTeams.Count; i++)
            {
                var player = totalPlayers.Find(e => e == pairTeams[i]);
                player.JoinInQueueTime = DateTime.Now.AddSeconds(i);
                player.IsInQueue = true;
            }

        }
    }
}
