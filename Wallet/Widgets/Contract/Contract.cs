using System;
using System.Text;
using Gtk;

namespace Wallet
{
	public interface ContractView {
		Boolean IsActive { set; }
		byte[] Hash { set; }
		ulong Tokens { set; }
		String Code { get; set; }
		String Assertion { set; }
		String Proof { set; }
	}

	[System.ComponentModel.ToolboxItem (true)]
	public partial class Contract : Bin, ContractView
	{
		public Contract ()
		{
			this.Build ();
			ContractController.Instance.ContractView = this;

			txtStatus.ModifyBase (StateType.Normal, new Gdk.Color (0x01d, 0x025, 0x030));
			txtContractCode.ModifyBase (StateType.Normal, new Gdk.Color (0x01d, 0x025, 0x030));
			textview3.ModifyBase (StateType.Normal, new Gdk.Color (0x01d, 0x025, 0x030));

			txtStatus.ModifyText (StateType.Normal, new Gdk.Color (0x0F7, 0x0F7, 0x0F7));
			txtContractCode.ModifyText (StateType.Normal, new Gdk.Color (0x0F7, 0x0F7, 0x0F7));
			textview3.ModifyText (StateType.Normal, new Gdk.Color (0x0F7, 0x0F7, 0x0F7));

			labelStatus.ModifyFg(Gtk.StateType.Normal, Constants.Colors.Text.Gdk);

			eventboxCreateOrExtend.ButtonPressEvent += delegate {
				ContractController.Instance.CreateOrExtend();
			};

			bool isGenerateBusy = false;
			eventboxValidate.ButtonPressEvent += async (o, args) => {
				if (isGenerateBusy)
				{
					labelStatus.Text = "Generating, skipped.";
					return;
				}

				isGenerateBusy = true;
				labelStatus.Text = "Generating...";
				var contractGenerationData = await ContractController.Instance.Verify(Code);

				Gtk.Application.Invoke(delegate
				{
					Proof = BitConverter.ToString(contractGenerationData.Hints);
				});

				labelStatus.Text = "Done.";
				isGenerateBusy = false;
			};

			eventboxLoad.ButtonPressEvent += delegate {
				ContractController.Instance.Load();
			};

			buttonSave.Clicked += delegate {
				ContractController.Instance.Save();
			};

			txtContractCode.Buffer.Changed += txtContractCode_Changed;
		}

		void txtContractCode_Changed(object sender, EventArgs e)
		{
			var textView = sender as TextBuffer;

			ContractController.Instance.UpdateContractInfo(textView.Text);
		}

		public String Code { 
			get
			{
				return txtContractCode.Buffer.Text;
			}
			set
			{
				txtContractCode.Buffer.Text = value;
			} 
		}

		public String ContractCodeAssertion { 
			set {
				txtStatus.Buffer.Text = value;
			} 
		}

		public String Proof { 
			set {
				textview3.Buffer.Text = value;
			} 
		}

		bool _IsActive;
		public bool IsActive
		{
			set
			{
				_IsActive = value;
			}
		}

		string _Hash;
		public byte[] Hash
		{
			set
			{
				_Hash = BitConverter.ToString(value).Replace("-", string.Empty);
				txtContractHash.Text = _Hash;
				UpdateStatus();
			}
		}

		public string Assertion
		{
			set
			{
				throw new NotImplementedException();
			}
		}

		ulong _Tokens;
		public ulong Tokens
		{
			set
			{
				_Tokens = value;
				UpdateStatus();
			}
		}

		public void UpdateStatus()
		{
			var stringBuilder = new StringBuilder();

			stringBuilder.AppendLine("Contract hash: " + _Hash);
			stringBuilder.AppendLine("Status: " + (_IsActive ? "ACTIVE" : "INACTIVE"));

			if (_Tokens != 0)
			{
				stringBuilder.AppendLine($"You own {_Tokens} tokens issued by this contract");
			}

			txtStatus.Buffer.Text = stringBuilder.ToString();
		}
	}
}

