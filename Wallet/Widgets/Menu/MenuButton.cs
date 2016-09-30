using System;
using Gtk;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class MenuButton : WidgetBase
	{
		public MenuButton ()
		{
			this.Build ();

			Selected = false;

			Container.ButtonPressEvent += delegate {
				Select();
				FindParent<MenuBase>().Selection = Name;
			};
		}

		public void Select() {
			foreach (Widget widget in FindParent<Container>().Children) {
				if (widget is MenuButton) {
					((MenuButton)widget).Selected = widget.Name == Name;
				}
			}
		}
			
		public bool Selected { 
			set 
			{
				Container.ModifyBg(Gtk.StateType.Normal, value ? Constants.Colors.ButtonSelected : Constants.Colors.ButtonUnselected);

				FindChild<ImageButton>().SetBackground(Constants.Images.Button(Name, value));
			}
		}

		private Container Container { 
			get {
				return (Container)Children [0];
			}
		}
	}
}

