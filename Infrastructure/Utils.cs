using System;
using System.Text;

namespace Infrastructure
{
	public class Utils
	{
		public static string ExceptionToString(Exception exception)
		{
			Exception ex = exception;
			StringBuilder stringBuilder = new StringBuilder(128);

			while (ex != null)
			{
				stringBuilder.Append(ex.GetType().Name);
				stringBuilder.Append(": ");
				stringBuilder.Append(ex.Message);
				stringBuilder.AppendLine(ex.StackTrace);
				ex = ex.InnerException;
				if (ex != null)
				{
					stringBuilder.Append(" ---> ");
				}
			}

			return stringBuilder.ToString();
		}
	}
}
