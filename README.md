# Zen Protocol Alpha Documentation

Links:
 * [Gihub Repo](https://github.com/zenprotocol/zen-wallet)
 * [Website](https://www.zenprotocol.com)
 * [Blog](https://blog.zenprotocol.com)
 * [Telegram](https://t.me/zenprotocol)

## Youtube Tutorials

* [Windows installation walkthrough](https://www.youtube.com/watch?v=gJRmtX7UL0c){:target="_blank"}
* [Using the Alpha tutorial](https://www.youtube.com/watch?v=Pi9lK8dJeGU&t=6s){:target="_blank"}
* [Chinese tutorial](https://www.youtube.com/watch?v=siLrcqpoZhA){:target="_blank"}
* [TUI walkthrough](https://www.youtube.com/watch?v=GR4R_hNDxJU){:target="_blank"}

### Mac OSX

1. Install [Mono for Mac](https://download.mono-project.com/archive/5.0.1/macos-10-universal/MonoFramework-MDK-5.0.1.1.macos10.xamarin.universal.pkg)
2. Download and install [Zen.dmg](https://s3-us-west-2.amazonaws.com/zenprotocol-alpha/Zen.dmg)
3. NOTE: when opening the app for the first time, right click the icon of the app and then click open. You will then receive a notification along the lines of "Zen can't be opened because it is from an unidentified developer." - click the "Open" button.

To run the miner: 
- `cd /Applications/Zen.app/Contents/Resources/`
- `mono zen.exe -m`


### Linux (Debian/Ubuntu)

1. Install [Mono for Linux](http://www.mono-project.com/download/#download-lin)
2. Install [fsharp](http://fsharp.org/use/linux/)
3. Install libsodium-dev `sudo apt install libsodium-dev`
4. Install GTK `sudo apt install gtk2.0`
5. Run `sudo apt-get update`
6. Download and extract [zen.tar.gz](https://s3-us-west-2.amazonaws.com/zenprotocol-alpha/zen.tar.gz)
7. Open shell and navigate to your extracted zen folder
8. Run `mono zen.exe`

If you would like to run the wallet with an active miner do the following:
- `mono zen.exe -m`
- When your miner finds a block you will receive 1,000 kalapas (the smallest unit of the ZEN token). Check your Balance tab to find them.


### Windows

Download and install [ZenInstaller.exe](https://s3-us-west-2.amazonaws.com/zenprotocol-alpha/ZenInstaller.exe)


## Using the Alpha

The Alpha is a combination of using [alpha.zenprotocol.com](http://alpha.zenprotocol.com) and the desktop wallet.
- [alpha.zenprotocol.com](http://alpha.zenprotocol.com) includes the following:
  - [Contract explorer](http://alpha.zenprotocol.com) - for browsing existing contracts
  - [Contract templates](http://alpha.zenprotocol.com/ContractCreation)  for deploying your own contracts
  - [Faucet](http://alpha.zenprotocol.com/Faucet) - to get your hands on some Zen
  - [An oracle](http://alpha.zenprotocol.com/Oracle) - We have prepared for the use of the demo
- The desktop wallet: a full node, that connects to the Zen testnet and allows you to send, receive and sign transactions, and activate smart contracts.

In the following, we use (Product/tab) to refer to the product and tab in which to perform the next action.

For example:
(Wallet/Portfolio) - refers to the desktop wallet app in the 'Portfolio' tab
(Web/Faucet) - refers to the alpha.zenprotocol.com website in the 'Faucet' page

### Usage Instructions

1. Get Some ZEN tokens
    - (Wallet/Wallet) Copy your address (you can click the copy button on the right hand side)
    - ([Web/Faucet](http://alpha.zenprotocol.com/Faucet)) - paste your address and click submit
    - (Wallet) In the footer of the wallet check that your client has finished syncing and downloaded the tip of the block chain. It should say "Blockchain synced. Last block ##"
    - (Wallet/Portfolio) You should now have 50,000,000 kalapas (which is 0.5 ZEN) (If you don't see it yet - check in the footer if your client finished syncing blocks)
2. Create a call option
    - ([Web/Templates](http://alpha.zenprotocol.com/ContractCreation)) - Click 'Create' on the [Vanilla Call Option](http://alpha.zenprotocol.com/ContractCreation/FromTemplate/CallOption) Template
    - (Web/Vanilla Call Option) Fill in the fields and click 'Create'
        - Name - give it any name you like
        - Numeraire - is currently hard coded to the ZEN token address
        - Oracle - Is hard coded to a the Zen oracle created for the alpha
        - Underlying - choose the stock ticker you want to issue call options for
        - Choose from the tickers our [oracle](http://alpha.zenprotocol.com/Oracle) is creating commitments to
        - Premium - is the amount of Kalapas (the smallest unit of ZEN) required to purchase one call option
        - Strike - is the price at which the buyer of a call option can exercise the option later (make the strike lower than the current price of the ticker in order to realize a profit)
        - Click 'Create'
    - (Web/Contract Details) Copy the contract code
    - (Wallet/Contract) Activate the contract
        - Paste the contract code in the in the "Code" field (You can use the "Paste" button) on the right
        - You can compare the Hash in the "Hash" field to the one in the browser to check you copied the code correctly
        - In the "Blocks" field - Choose for how many blocks you would like to activate your contract (50 blocks should be good)
        - Click the 'Active' button
    - ([Web/Explorer](http://alpha.zenprotocol.com)) wait until you see that your new contract is active under the "Active Until" row - if it displays a number you are good. If it says "Inactive" try refreshing until it displays a number
    4. Collateralize the call option - In order to allow people to purchased collateralized call options we must first collaterlize the contract.
        - (Web) Click on your newly created contract
        - (Web) Click on the 'Collateralize' button
        - (Web/Transaction Details) Copy the 'Address' field
        - (Wallet/Wallet) Click on the 'Send' button
        - (Wallet/Send) Paste your address in the address field
        - (Web/Transaction Details) Copy the 'Data' field
        - (Wallet/Send) Paste your data in the 'Data' field
        - (Wallet/Send) Amount to send: enter 100,000 - if your premium was 50 - then the contract will be able to issue 20,000 options
        - (Wallet/Send) Click the 'Sign & Review' button
        - (Web/Call Option Page) If you refresh the page of your contract you should now see "Buy" and "Exercise" buttons
    5. Buy some call options
        - (Web/Call Option Page) Go to your call option page
        - (Web/Call Option Page) Click on the 'Buy' button
        - (Wallet/Wallet) Copy your address
        - (Web) Paste your address in the 'Send to wallet address' field and click 'Submit'
        - (Web/Transaction Details) Copy the 'Address' field
        - (Wallet/Wallet) Click on the 'Send' button
            - (Wallet/Send) Paste your address in the address field
            - (Web/Transaction Details) Copy the 'Data' field
            - (Wallet/Send) Paste your data in the data field
            - (Wallet/Send) Select asset - choose 'Zen'
            - (Wallet/Send) Amount to send - if the premium is 50 - then enter an amount devisable by 50 - the contract then will issue you option tokens representing your claim for the collateral in the contract in the case that the price of the ticker is above the strike price
            - (Wallet/Send) Click the 'Sign & Review' button
        - (Wallet/Portfolio) You should now see that you have the call option tokens in your portfolio
    6. Exercise the option
        - (Web) Go to the call option page
        - (Web) Click the 'Exercise' button
        - (Wallet/Wallet) Copy your wallet address
        - (Web) Paste your address in the 'Return address' field and click 'Submit'
        - (Web/Transaction Details) Copy the 'Address' field
        - (Wallet/Wallet) Click on the 'Send' button
        - (Wallet/Send) Paste your address in the address field
        - (Web/Transaction Details) Copy the 'Data' field
        - (Wallet/Send) Paste your data in the data field
        - (Wallet/Send) Select asset - choose the call option token - since you are now sending it back to the contract in order to exercise it
        - (Wallet/Send) Amount to send - choose how many options you want to exercise
        - (Wallet/Send) Click the 'Sign & Review' button
        - (Wallet/Portfolio) You should now see you have less call option tokens
        - (Wallet/Balance) You should see a transaction for sending your call option tokens
        - (Wallet/Balance) Choose the 'Zen' token in the 'Asset' dropdown - you should see a new incoming transaction of your profit from exercising the option.
