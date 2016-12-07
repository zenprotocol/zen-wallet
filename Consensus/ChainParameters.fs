module Consensus.ChainParameters


[<Measure>] type zen
[<Measure>] type kalapa

// Will be lower in mainnet
[<Literal>]
let MaxZen = 1000000000L<zen>

// Likely to remain the same in mainnet
[<Literal>]
let KalapasPerZen = 100000000L<kalapa/zen>

let MaxKalapas = MaxZen * KalapasPerZen

// multiplier will be lower in mainnet
let TotalMinerRewardZen = MaxZen * 1L

let TotalMinerReward = TotalMinerRewardZen * KalapasPerZen

[<Measure>] type sec
[<Measure>] type year

// Based on mean tropical year in the 21st century
let secondsPerYear = 31556925.<sec/year>

let blockInterval = 300.<sec>

// Will change in mainnet
let initialRewardZen = 500L<zen>
let initialReward = initialRewardZen * KalapasPerZen

let blocksPerHalvingPeriod = uint32 <| (TotalMinerReward / initialReward) / 2L

let halvingPeriod = blockInterval * float blocksPerHalvingPeriod

let totalRewardUpToPeriod (n:uint32) = TotalMinerReward - 1L<kalapa> * int64 (float TotalMinerReward * pown 0.5 (int n))
let totalRewardInPeriod (n:uint32) = totalRewardUpToPeriod (n+1u) - totalRewardUpToPeriod n

let totalRewardL = List.map totalRewardInPeriod [0u..50u]

let perBlockRewardL = List.map (fun r -> r / int64 blocksPerHalvingPeriod) totalRewardL
let rewardPerBlockInPeriod (n:uint32) = if int n < perBlockRewardL.Length then perBlockRewardL.Item (int n) else 0L<kalapa>

let periodOfBlock n = uint32 (n / blocksPerHalvingPeriod)

let rewardInBlock n = rewardPerBlockInPeriod (periodOfBlock n)

