using Wallet;
using System;
using Gtk;

public partial class MainWindow: Gtk.Window, IListener
{
	private MainController mainController = MainController.GetInstance();

	public MainWindow () : base (Gtk.WindowType.Toplevel)
	{
		Build ();

		mainController.AddListener (this);
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		mainController.Quit ();
		Application.Quit ();
		a.RetVal = true;
	}

	public void UpdateUI(DataModel dataModel) {
		Gtk.Application.Invoke(delegate {
			lblOne.Text = dataModel.DecimalOne.ToString();
			lblTwo.Text = dataModel.DecimalTwo.ToString();
		});
	}

	protected void btnTest_Clicked (object sender, EventArgs e)
	{
		//TODO: will this method run on the "UI thread"?
		mainController.TestMethod ();
	}

	protected void menuSignAndVerify_clicked (object sender, EventArgs e)
	{
		String result = "test";

		try {
			//TestClass testClass = new TestClass();

			//result = testClass.result;
		} catch (Exception ex) {
			result = ex.Message;
		}

		MessageDialog md = new MessageDialog(this, 
			DialogFlags.DestroyWithParent, MessageType.Info, 
			ButtonsType.Close, result);
		
		md.Run();
		md.Destroy();
	}
}
