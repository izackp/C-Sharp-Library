using System;
using System.Linq;

public static class StringExt
{
	public static bool IsAlphaNum(this string str)
	{
		if (string.IsNullOrEmpty(str))
			return false;
		
		return (str.ToCharArray().All(c => Char.IsLetter(c) || Char.IsNumber(c)));
	}

	public static string RemoveWhitespace(this string input)
	{
		return new string(input.ToCharArray()
		                  .Where(c => !Char.IsWhiteSpace(c))
		                  .ToArray());
	}
}