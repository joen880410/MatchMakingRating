/* 
    簡化版 TrueSkill 系統 - 程式碼說明 Summary
    -------------------------------------------
    1. 每個玩家以一個正態分布來表示其實力，包含均值 (μ) 與不確定性 (σ)。
    2. 初始時，每個玩家預設有一組 μ 與 σ（例如：μ=25.0, σ=8.333）。
    3. 比賽結果由一個排序好的玩家清單表示，清單中第 0 位為冠軍。
    4. 更新機制採用兩兩配對方式，認為排名較前的玩家戰勝排名較後的玩家。
    5. 更新公式利用標準常態分布的機率密度函數 (PDF) 及累積分布函數 (CDF)
       計算 v(t) 與 w(t) 值，並根據這些值來調整每位玩家的 μ 與 σ。
    6. 此程式碼為簡化版，並非完整的 TrueSkill 實作，但已保留其核心概念。
*/

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

namespace SimpleTrueSkillDemo
{
    // 玩家類別：每個玩家有識別碼、均值 (μ) 與不確定性 (σ)
    public class Player
    {
        public int rank { get; set; }
        public string Id { get; set; }      // 玩家識別碼
        public double Mu { get; set; }      // 玩家實力的均值
        public double Sigma { get; set; }   // 玩家實力的不確定性

        // 建構子：初始化玩家，並設定預設的 μ 與 σ
        public Player(string id, double mu = 25.0, double sigma = 8.333)
        {
            Id = id;
            Mu = mu;
            Sigma = sigma;
        }

        // 輸出玩家資訊，方便列印
        public override string ToString() => $"{Id}: μ = {Mu:F2}, σ = {Sigma:F2}";
    }

    // SimpleTrueSkill 類別：包含主要的更新邏輯
    public static class SimpleTrueSkill
    {
        // Beta 參數代表比賽表現波動，可根據遊戲需求調整
        public const double Beta = 4.1667;
        // 設定 sigma 的最小值，避免評分過於精確（不確定性過低）
        public const double MinSigma = 1.0;

        // 標準常態分布機率密度函數 (PDF)
        public static double Phi(double x) =>
            Math.Exp(-x * x / 2) / Math.Sqrt(2 * Math.PI);

        // 標準常態分布累積分布函數 (CDF)
        // 這裡採用簡單的近似公式進行計算
        public static double PhiCDF(double x)
        {
            double k = 1.0 / (1.0 + 0.2316419 * Math.Abs(x));
            double poly = k * (0.319381530 + k * (-0.356563782 + k * (1.781477937 + k * (-1.821255978 + k * 1.330274429))));
            double approx = 1.0 - (1.0 / Math.Sqrt(2 * Math.PI)) * Math.Exp(-0.5 * x * x) * poly;
            return x < 0 ? 1.0 - approx : approx;
        }

        // v 函數：計算 v(t) = φ(t) / Φ(t)
        public static double V(double t)
        {
            //平局臨界值
            double ε = 1e-10;
            double denom = PhiCDF(t);
            // 避免分母為零的情況
            return denom < ε ? -t : Phi(t) / denom;
        }

        // w 函數：計算 w(t) = v(t) * (v(t) + t)
        public static double W(double t)
        {
            double v = V(t);
            return v * (v + t);
        }

        // UpdatePair 方法：根據一場比賽中 winner 戰勝 loser 的結果，更新其 μ 與 σ
        public static void UpdatePair(Player winner, Player loser)
        {
            var c = UnCertainty(winner, loser);
            var t = DifferenceMu(winner, loser, c);
            // 計算更新時需要的 v 與 w 值
            double v = V(t);
            double w = W(t);

            // 更新 μ：勝者增加分數，敗者扣分；更新幅度根據各自的 σ 與 v 調整
            double deltaWinner = (winner.Sigma * winner.Sigma / c) * v;
            double deltaLoser = (loser.Sigma * loser.Sigma / c) * v;
            winner.Mu += deltaWinner;
            loser.Mu -= deltaLoser;
            // 更新 σ：根據 w 調整不確定性，並確保不低於 MinSigma
            winner.Sigma = Math.Max(Math.Sqrt(winner.Sigma * winner.Sigma * (1 - (winner.Sigma * winner.Sigma) / (c * c) * w)), MinSigma);
            loser.Sigma = Math.Max(Math.Sqrt(loser.Sigma * loser.Sigma * (1 - (loser.Sigma * loser.Sigma) / (c * c) * w)), MinSigma);
        }

        // UpdateRatings 方法：根據比賽結果（已排序的玩家清單）更新所有玩家的評分
        // 清單順序中，第 0 位為冠軍，依序往下，採用全配對更新的方式
        public static void UpdateRatings(List<Player> players)
        {
            int n = players.Count;
            // 兩層迴圈對每一對玩家進行配對更新
            for (int i = 0; i < n; i++)
            {
                Player player = players[i];
                player.rank = i + 1;
                if (i + 1 < n)
                {
                    players[i + 1].rank = i + 2;
                }

                for (int j = i + 1; j < n; j++)
                {
                    UpdatePair(players[i], players[j]);
                }
            }
        }
        public static double UnCertainty(Player player, Player player1)
        {
            // 計算合併後的不確定性 (c)，包含 Beta 以及雙方的 σ
            return Math.Sqrt(2 * Beta * Beta + player.Sigma * player.Sigma + player1.Sigma * player1.Sigma);
        }
        public static double DifferenceMu(Player player, Player player1, double c)
        {
            // 根據兩人目前的 μ 差距計算 t
            return (player.Mu - player1.Mu) / c;
        }
    }

    // 主程式：建立玩家、模擬比賽結果、並更新玩家評分
    class Program
    {
        static void Main(string[] args)
        {
            // 建立玩家清單，每位玩家具有預設的 μ 與 σ
            List<Player> players = new List<Player>
            {
                new Player("A"),
                new Player("B"),
                new Player("C"),
                new Player("D"),
                new Player("E"),
                new Player("F"),
                new Player("G"),
                new Player("H")
            };


            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine("比賽前玩家評分：");
                for (int j = 0; j < players.Count - 1; j++)
                {
                    int n = players.Count;
                    Player player = players[j];
                    player.rank = j + 1;

                    if (j + 1 < n)
                    {
                        players[j + 1].rank = j + 2;
                    }

                    for (int k = j + 1; k < n; k++)
                    {
                        var c = SimpleTrueSkill.UnCertainty(player, players[k ]);
                        var t = SimpleTrueSkill.DifferenceMu(player, players[k], c);
                        var matchScore = (Math.Exp(-t / 2 * t) * Math.Sqrt(2 * SimpleTrueSkill.Beta * SimpleTrueSkill.Beta / (c * c))).ToString("F5");
                        Console.WriteLine($"{player.Id} Vs {players[k].Id} score:{matchScore}");
                    };
                }
                // 模擬一次比賽結果：
                // 假設比賽名次為：C 第一、A 第二、D 第三、B 第四
                // 清單中順序代表名次，索引 0 為冠軍
                List<Player> raceResult = new List<Player>
                {
                    players[2], // C
                    players[0], // A
                    players[3], // D
                    players[1], // B
                    players[2], // C
                    players[0], // A
                    players[3], // D
                    players[1]  // B
                };

                // 根據比賽結果更新所有玩家的評分
                SimpleTrueSkill.UpdateRatings(players);

                Console.WriteLine("\n比賽後玩家評分：");
                players.ForEach(p => Console.WriteLine(p));
            }


            Console.WriteLine("\n按任意鍵退出...");
            Console.ReadKey();
        }
    }
}
