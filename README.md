Simple C# console application which exports [Codeforces](http://codeforces.ru) contests to Google Calendar events.

Has a feature of filtering exported contests by division.

Requires [HtmlAgilityPack](http://htmlagilitypack.codeplex.com/) and [Google Data API](http://code.google.com/p/google-gdata/) libraries.

Credentials for Google Account, calendar and division filtering can be set up in app.config (after renaming app.config.dist to app.config).