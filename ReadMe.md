## 概念
TrueSkill 是由 Microsoft 提出的用於多人遊戲的評分系統，旨在根據比賽結果動態調整玩家的實力評分。其核心概念是使用 正態分佈 來對每個玩家的實力進行建模，並根據勝負結果更新玩家的「估計實力」和「不確定性」。
## 公式
<p>- TrueSkill 更新公式</p>
μ_winner ← μ_winner + (σ_winner² / c) * v((μ_winner - μ_loser) / c, ε / c)
μ_loser ← μ_loser - (σ_loser² / c) * v((μ_winner - μ_loser) / c, ε / c)

σ_winner² ← σ_winner² * [1 - (σ_winner² / c) * w((μ_winner - μ_loser) / c, ε / c)]
σ_loser² ← σ_loser² * [1 - (σ_loser² / c) * w((μ_winner - μ_loser) / c, ε / c)]

t = (μ_winner - μ_loser) / c

c² = 2β² + σ_winner² + σ_loser²

V(t) = φ(t) / Φ(t) # 概率分佈函數/累積分佈函數

W(t) = V(t)*(V(t)+t)# 

<p>- 匹配值計算公式</p>
e⁽⁻⁽ᵐᵘᴬ ⁻ ᵐᵘᴮ⁾² /²ᶜ²⁾ ⋅ √d

d = 2β²/c²

---
# 參考鏈接
- 1 [Trueskill中文參考](https://www.gameres.com/908465.html)
- 2 [Trueskill英文參考](https://web.archive.org/web/20110605140455/http://research.microsoft.com/en-us/projects/trueskill/details.aspx)