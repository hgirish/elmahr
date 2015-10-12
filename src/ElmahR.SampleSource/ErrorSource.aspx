<%@ Page Language="C#" Title="ElmahR Dashboard errors generator" EnableViewState="false" %>
<%@ Import Namespace="Elmah" %>
<%@ Import Namespace="ElmahR.SampleSource.Dep" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<script runat="server">

    protected void ErrorButton_Click(object sender, EventArgs e)
    {
        ThrowSampleException();
    }

    protected void SignalErrorButton_Click(object sender, EventArgs e)
    {
        SignalMessage.InnerText = "Trapping error at "
            + DateTime.Now.ToLongTimeString();

        try
        {
            BuggyClass.RaiseException();
        }
        catch (Exception ex)
        {
            ErrorSignal.FromContext(Context).Raise(ex);
        }

        SignalMessage.DataBind();
    }

    private static void ThrowSampleException()
    {
        var exceptions = new Action[]
            {
                () => { throw new System.ApplicationException(); }, 
                () => { throw new ArgumentException(); }, 
                () => { throw new ArgumentNullException(); }, 
                () => { throw new InvalidCastException(); }, 
                () => { throw new NullReferenceException(); }, 
                () => { throw new AccessViolationException(); }, 
                () => { throw new HttpException(); }, 
            };

        var r = new Random(DateTime.Now.Millisecond);
        exceptions[r.Next(exceptions.Length)]();
    }

</script>
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <meta http-equiv="X-UA-Compatible" content="IE=EmulateIE7" />
    <title>ElmahR Dashboard errors generator</title>
    <style type="text/css">
        body
        {
            background-color: white;
        }
        body, td, th, input, select
        {
            font-family: Arial, Sans-Serif;
            font-size: small;
        }
        code, pre
        {
            font-family: Courier New, Courier, Monospace;
            font-size: small;
        }
        li
        {
            margin-bottom: 0.5em;
        }
        dd
        {
            margin-bottom: 2em;
        }
        dt
        {
            font-weight: bold;
        }
        #SignalMessage
        {
            color: #F00;
        }
        h1
        {
            font-size: large;
        }
        h2
        {
            font-size: medium;
        }
        h3
        {
            font-size: small;
        }
    </style>
</head>
<body>
    <form id="Form1" runat="server">
    <h1><%= Server.HtmlEncode(Title) %></h1>
    <h2>Introduction</h2>
    <p>
        This sample web page generates errors
        that ElmahR will show in real time
        as they happen.
    </p>
    <h2>Generate Exceptions!</h2>
    <h3>Unhandled Exceptions</h3>
    <p>
        Click the button below to generate an exception. The exception
        will not be handled by this sample application. As a result,
        ELMAH will log the error and <em>send</em> it to ElmahR, 
        which will show it in the dashboard. Bear in mind that 
        the exception will generate what has come to be known as 
        the <em><a href="http://en.wikipedia.org/wiki/Yellow_Screen_of_Death#ASP.NET">yellow screen of death</a></em>
        that ASP.NET developers often dread. You will need to hit the 
        &ldquo;back&rdquo; button on your browser to return here.</p>
    <p>
        <asp:Button ID="ErrorButton" runat="server" Text="Throw Exception" OnClick="ErrorButton_Click" />
    </p>

    <h3>Handled Exceptions</h3>
    <p>
        Click the button below to <em>signal</em> an exception to ELMAH.
        As for unhandled exceptions, ELMAH will take care of it and 
        <em>send</em> it to ElmahR, which will show it in the dashboard.
    </p>
    <p>
        <asp:Button ID="SignalButton" runat="server" Text="Signal Handled Exception" OnClick="SignalErrorButton_Click" />
        <span ID="SignalMessage" runat="server" visible="<%# !string.IsNullOrEmpty(SignalMessage.InnerText) %>" />
    </p>
    </form>
</body>
</html>
