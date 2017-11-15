# Zen Protocol Alpha 文档

链接:
 * [Gihub Repo](https://github.com/zenprotocol/zen-wallet)
 * [Website](https://www.zenprotocol.com)
 * [Blog](https://blog.zenprotocol.com)
 * [Telegram](https://t.me/zenprotocol)

## 安装指南

[YouTube 视频教程](https://www.youtube.com/watch?v=Pi9lK8dJeGU)

### Mac OSX

1. 安装 [Mono for Mac](https://download.mono-project.com/archive/5.0.1/macos-10-universal/MonoFramework-MDK-5.0.1.1.macos10.xamarin.universal.pkg)
2. 下载并安装 [Zen.dmg](https://s3-us-west-2.amazonaws.com/zenprotocol-alpha/Zen.dmg)
3. 注意：当第一次打开Zen应用时，右键单击应用图标然后点击“open”，之后会弹出一个通知内容是“Zen can't be opened because it is from an unidentified developer.”，点击“Open”按钮。

运行挖矿程序: 
- `cd /Applications/Zen.app/Contents/Resources/`
- `mono zen.exe -m`


### Linux (Debian/Ubuntu)

1. 安装 [Mono for Linux](http://www.mono-project.com/download/#download-lin)
2. 安装 [fsharp](http://fsharp.org/use/linux/)
3. 安装 libsodium-dev `sudo apt install libsodium-dev`
4. 安装 GTK `sudo apt install gtk2.0`
5. 运行 `sudo apt-get update`
6. 下载并解压缩 [zen.tar.gz](https://s3-us-west-2.amazonaws.com/zenprotocol-alpha/zen.tar.gz)
7. 打开shell并前往解压后的zen文件夹
8. 运行 `mono zen.exe`

执行下面命令来运行带有挖矿功能的钱包:
- `mono zen.exe -m`
- 当矿机发现一个区块，你将会收到 1000 kalapas (ZEN代币的最小单位)，在钱包的“Balance”标签内可以找到。


### Windows

下载并安装 [ZenInstaller.exe](https://s3-us-west-2.amazonaws.com/zenprotocol-alpha/ZenInstaller.exe)


## 使用Alpha版

通过[alpha.zenprotocol.com](http://alpha.zenprotocol.com)和桌面钱包功能来使用Alpha版本。
- [alpha.zenprotocol.com](http://alpha.zenprotocol.com) 包含下面的内容:
  - [合约浏览器](http://alpha.zenprotocol.com) - 用来浏览已经存在的合约
  - [合约模板](http://alpha.zenprotocol.com/ContractCreation) - 用来部署你自己的合约
  - [水龙头(Faucet)](http://alpha.zenprotocol.com/Faucet) - 用来获取一些测试的ZEN代币
  - [预言机(An oracle)](http://alpha.zenprotocol.com/Oracle) - 我们准备了一些预言机对Alpha版本进行测试
- 桌面钱包是一个全功能节点，可以连接Zen测试网络，允许你对交易进行发送、接收、签名，并且激活智能合约。

下面我们使用应用/标签来帮助你理解应该转到哪一个应用和标签。

例如:
(Wallet/Portfolio) - 指示的是桌面钱包应用的“Portfolio”标签
(Web/Faucet) - 指示的是 alpha.zenprotocol.com 网站的“Faucet”页面

### 使用指南

1. 获取一些ZEN代币
    - (Wallet/Wallet) 复制你的地址 (可以点击右边的复制按钮进行复制)
    - ([Web/Faucet](http://alpha.zenprotocol.com/Faucet)) - 粘贴你的地址并点击submit
    - (Wallet) 在钱包的页脚检查客户端是否完成了区块的同步。应该显示"Blockchain synced. Last block ##"
    - (Wallet/Portfolio) 你应该可以看到有 50,000,000 kalapas (即 0.5 ZEN) (如果还没有出现，检查页脚看看你的客户端是否完成了同步)
2. 创建看涨期权
    - ([Web/Templates](http://alpha.zenprotocol.com/ContractCreation)) - 在 [Vanilla Call Option](http://alpha.zenprotocol.com/ContractCreation/FromTemplate/CallOption) Template，点击'Create'
    - (Web/Vanilla Call Option) 填写表单，然后点击'Create'
        - Name - 起一个你想要的名字
        - Numeraire - 当前为固定的ZEN代币地址
        - Oracle - 为Alpha版创建的固定Zen预言机
        - Underlying - 选择用来发行看涨期权的股票行情自动收录器
        - 从我们的 [oracle](http://alpha.zenprotocol.com/Oracle) 所委托的行情自动收录器中选择
        - Premium - 是购买一个看涨期权所需的 Kalapas (ZEN代币的最小单位)数量
        - Strike - 是一个看涨期权的购买者之后能够行权的价格(让strike价格低于行情自动收录器的当前价格，才能实现获利)
        - 点击 'Create'
    - (Web/Contract Details) 复制合约代码
    - (Wallet/Contract) 激活合约
        - 在“Code”一栏中粘贴合约代码(可以使用粘贴按钮)
        - 比较“Hash”一栏中的哈希值和浏览器中的哈希值是否同一个，来检查是否正确的复制了代码
        - 在 "Blocks" 一栏 - 选择你想激活合约的区块数量 (50个区块就可以)
        - 点击 'Active' 按钮
    - ([Web/Explorer](http://alpha.zenprotocol.com)) 等着直到在 "Active Until" 行下看见新创建的合约变成有效的 - 如果显示一个数字，就证明可以了。 如果显示 "Inactive" 尝试刷新直到显示数字。
3. 抵押看涨期权 - 为了能够让别人购买抵押的看涨期权，我们必须首先以这份合约作为抵押。
    - (Web) 点击你刚刚创建的合约
    - (Web) 点击'Collateralize'按钮
    - (Web/Transaction Details) 复制'Address'一栏
    - (Wallet/Wallet) 点击'Send'按钮
    - (Wallet/Send) 粘贴地址进地址栏
    - (Web/Transaction Details) 复制'Data'一栏
    - (Wallet/Send) 粘贴数据进'Data'栏
    - (Wallet/Send) Amount to send: 输入 100,000 - 如果你的溢价是50，那么合约可以发行20,000份期权
    - (Wallet/Send) 点击'Sign & Review'
    - (Web/Call Option Page) 刷新你的合约页面，应该可以看到"Buy"和"Exercise"按钮
4. 购买看涨期权
    - (Web/Call Option Page) 转到看涨期权页面
    - (Web/Call Option Page) 点击 'Buy' 按钮
    - (Wallet/Wallet) 复制你的地址
    - (Web) 粘贴地址到 'Send to wallet address' 一栏并点击 'Submit'
    - (Web/Transaction Details) 复制 'Address' 一栏
    - (Wallet/Wallet) 点击 'Send' 按钮
        - (Wallet/Send) 粘贴你的地址进地址栏
        - (Web/Transaction Details) 复制 'Data' 一栏
        - (Wallet/Send) 粘贴你的数据进数据栏
        - (Wallet/Send) 选择资产 - 选择 'Zen'
        - (Wallet/Send) Amount to send - 如果溢价是50，那么输入可以被50整除的数字（如100、150等）。之后合约会向你发行期权令牌，在行情自动收录器中的价格高于执行价格情况下，这些令牌代表了对合约中抵押物的索赔权益。
        - (Wallet/Send) 点击 'Sign & Review' 按钮
    - (Wallet/Portfolio) 现在应该能够在资产组合页内看到看涨期权令牌
5. 行权
    - (Web) 转到看涨期权合约页面
    - (Web) 点击 'Exercise' 按钮
    - (Wallet/Wallet) 复制钱包地址
    - (Web) 粘贴地址到 'Return address' 一栏并点击 'Submit'
    - (Web/Transaction Details) 复制 'Address' 一栏
    - (Wallet/Wallet) 点击 'Send' 按钮
    - (Wallet/Send) 粘贴地址到地址栏
    - (Web/Transaction Details) 复制 'Data' 一栏的值
    - (Wallet/Send) 粘贴数据到数据栏
    - (Wallet/Send) Select asset - 选择看涨期权令牌，现在你要把它发送回合约，从而实现行权
    - (Wallet/Send) Amount to send - 选择你想要行权的期权数量
    - (Wallet/Send) 点击 'Sign & Review' 按钮
    - (Wallet/Portfolio) 现在你应该注意到拥有的看涨期权令牌变少了
    - (Wallet/Balance) 应当能够看到一个发送看涨期权令牌的交易
    - (Wallet/Balance) 在 'Asset' 下拉框选择 'Zen' 令牌，应当能够看到一笔行权产生的盈利收入交易。
