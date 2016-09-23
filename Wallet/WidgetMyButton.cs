using System;

namespace Wallet
{
	public delegate void MyEventHandler (String info);

	[System.ComponentModel.ToolboxItem (true)]
	public partial class WidgetMyButton : Gtk.Bin
	{
//		public event MyEventHandler MyButtonClicked;

		public WidgetMyButton ()
		{
			this.Build ();

			Selected = false;

			eventbox9.ButtonPressEvent += delegate {
				EventBus.GetInstance().Dispatch("button", Name);
//				Console.WriteLine("clicked " + Name);
//				if (MyButtonClicked != null)
//				{
//					MyButtonClicked("mmm");
//				}



				Select();
			};

			//	EventBus.GetInstance().Register(ClassTag, String val);
			//EventBus.GetInstance().RegisterIncomingFromParent(this, (String xnnn) => {

			//});
		}

		public void Select() {
			foreach (Gtk.Widget widget in ((Gtk.Container)Parent).Children) {
				if (widget is WidgetMyButton) {
					((WidgetMyButton)widget).Selected = widget.Name == Name;
				}
			}
		}

		public bool Selected { 
			set 
			{
				eventbox9.ModifyBg(Gtk.StateType.Normal, !value ? new Gdk.Color(0x01d,0x025,0x030) : new Gdk.Color(0x028,0x02f,0x037));
				WidgetButtonContent WidgetButtonContent = GetWidgetButtonContent ();

				String asset = "Wallet.Assets." + Name + (value ? "_on.png" : "_off.png");
				try {
					WidgetButtonContent.SetBackground(asset);
				} catch (Exception e) {
					Console.WriteLine("missing" + asset);
				}
			}
		}

		private WidgetButtonContent GetWidgetButtonContent() {
			Gtk.Container c = (Gtk.Container) Children[0];
			c = (Gtk.Container) c.Children [0];	
			c = (Gtk.Container) c.Children [0];	

			return (WidgetButtonContent)c.Children [0];
		}
	}
}

