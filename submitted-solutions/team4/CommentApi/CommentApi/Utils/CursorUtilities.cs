
namespace CommentApi.Utils;
public class CursorUtilities
{
    public static (int level, string cursorPrefix, int topLevelOffset) ParseCursor(string cursor)
    {
        string cursorPrefix;
        int topLevelOffset;
        var level = cursor.Count(ch => ch == '/') + 1;
        var lastIndexOf = cursor.LastIndexOf("/", StringComparison.Ordinal);
        if (lastIndexOf > 0)
        {
            cursorPrefix = cursor.Substring(0, lastIndexOf + 1);
            topLevelOffset = int.Parse(cursor.Substring(lastIndexOf + 1));
        }
        else
        {
            cursorPrefix = "";
            topLevelOffset = int.Parse(cursor);
        }

        return (level, cursorPrefix, topLevelOffset);
    }

    public static string CreateRepliesCursor(string cursor) => $"{cursor}/0";
}
