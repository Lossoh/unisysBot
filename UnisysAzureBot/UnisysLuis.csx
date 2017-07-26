using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Luis.Models;
using System.Globalization;
using Newtonsoft.Json;

[Serializable]
[LuisModel("012b9a65-2d83-4d55-9637-083b8fe9f7d7", "49c7fa25dbe643599f851cecf1c0d724")]
public class UnisysLuis : LuisDialog<object>
{
    //Global members
    //string strCurrentURL = System.Configuration.ConfigurationManager.AppSettings["Bot_Publish_Url"];
    string strCurrentURL = "https://unisysblobstorageclient.blob.core.windows.net";
    private const string strYes = "Yes, I'm using I.E. 11";
    private const string strNo = "No, I'm using another browser";
    [LuisIntent("")]
    [LuisIntent("None")]
    public async Task None(IDialogContext context, LuisResult result)
    {
        await context.PostAsync("Sorry, I am not getting you...");
        context.Wait(this.MessageReceived);
    }

    [LuisIntent("Welcome")]
    public async Task Welcome(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
    {
        await context.PostAsync(WelcomeMessage());
        context.Wait(this.MessageReceived);
    }

    [LuisIntent("Issue")]
    [LuisIntent("Help")]
    [LuisIntent("IEIssue")]
    [LuisIntent("OutlookIssue")]
    public async Task Issue(IDialogContext context, LuisResult result)
    {
        string strIssueType = "";
        if (result.Intents.Count > 0)
        {
            strIssueType = result.Intents[0].Intent.ToLower();
            strIssueType = strIssueType.Substring(0, strIssueType.Length - 5);
        }

        context.UserData.SetValue<string>(ContextConstants.IssueType, strIssueType);
        await context.PostAsync($"More than happy to assist you. Kindly, elaborate in detail about your {strIssueType} issue.");
        context.Wait(this.MessageReceived);
    }

    [LuisIntent("HybridIntent")]
    public async Task HybridIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"Please give details about one issue at a time. Currently we are serving outlook issue");
    }

    [LuisIntent("Thanks")]
    public async Task Thanks(IDialogContext context, LuisResult result)
    {
        await context.PostAsync($"It was a pleasure speaking with you. Have a Good Day!");
    }

    //Recovering deleted items in Outlook
    [LuisIntent("OutlookSolution")]
    [LuisIntent("OutlookOption")]
    public async Task OutlookOption(IDialogContext context, LuisResult result)
    {
        string sIssueTypeFromUser = "";
        if (result.Intents.Count > 0)
        {
            sIssueTypeFromUser = result.Intents[0].Intent.ToLower();
            sIssueTypeFromUser = sIssueTypeFromUser.Substring(0, sIssueTypeFromUser.Length - 8);
        }
        if (context.UserData.TryGetValue<string>(ContextConstants.IssueType, out string sIssueTypeFromSession) && sIssueTypeFromSession.Length > 0)
        {
            if (sIssueTypeFromUser != sIssueTypeFromSession)
                await context.PostAsync($"We are currently resolving {sIssueTypeFromSession} issue. Please give us some details about your {sIssueTypeFromSession} issue.");
        }
        else
            context.UserData.SetValue<string>(ContextConstants.IssueType, sIssueTypeFromUser);
        if (context.UserData.GetValue<string>(ContextConstants.IssueType).ToLower().Contains("outlook"))
        {
            await context.PostAsync($"Please launch Outlook and click Folder > Recover Deleted Items");
            await context.PostAsync(CreateImageAttachment(context, $"I'm attaching an image to find the tabs", "1.png", strCurrentURL));
            await context.PostAsync(CreateImageAttachment(context, $"Click the message you want to recover, and then click Recover Selected Items like below.", "2.png", strCurrentURL));
            await context.PostAsync(CreateImageAttachment(context, $"To select multiple items, press Ctrl as you click each item, and then click Recover Selected Items as shown below.", "3.png", strCurrentURL));
            context.UserData.SetValue<string>(ContextConstants.IssueType, "");
            PromptDialog.Choice(context, this.OnOptionSelected1, new List<string>() { "Yes", "No" }, "Did my recommendation solve your issue?", "Not a valid option", 1);
        }
    }


    private async Task OnOptionSelected1(IDialogContext context, IAwaitable<string> result)
    {

        string optionSelected = await result;

        switch (optionSelected)
        {
            case "Yes":
                PromptDialog.Choice(context, this.OnOptionSelected2, new List<string>() { "Yes", "No" }, "Is there anything else i can do for you today?", "Not a valid option", 1);
                break;

            case "No":
                await context.PostAsync(CreateAttachmentWithHeroCard(context, "", $"I am still learning. I will connect you with a live agent who can assist you further.", $"Connect Now", ActionTypes.OpenUrl, $"https://www.unisys.com/about-us/support/unisys-support-services"));
                await context.PostAsync($"It was a pleasure speaking with you. Have a Good Day!");
                break;
        }
    }

    private async Task OnOptionSelected2(IDialogContext context, IAwaitable<string> result)
    {

        string optionSelected = await result;

        switch (optionSelected)
        {
            case "Yes":
                await context.PostAsync($"Please elaborate your issue.");
                break;

            case "No":
                await context.PostAsync($"It was a pleasure speaking with you. Have a Good Day!");
                break;
        }

    }

    //IE Issue - webpage is loading slowly - Multimedia turn off
    [LuisIntent("IESolution")]
    public async Task IESolution(IDialogContext context, LuisResult result)
    {
        string sIssueTypeFromUser = "";
        if (result.Intents.Count > 0)
        {
            sIssueTypeFromUser = result.Intents[0].Intent.ToLower();
            sIssueTypeFromUser = sIssueTypeFromUser.Substring(0, sIssueTypeFromUser.Length - 8);
        }
        if (context.UserData.TryGetValue<string>(ContextConstants.IssueType, out string sIssueTypeFromSession) && sIssueTypeFromSession.Length > 0)
        {
            if (sIssueTypeFromUser != sIssueTypeFromSession)
                await context.PostAsync($"We are currently resolving {sIssueTypeFromSession} issue. Please give us some details about your {sIssueTypeFromSession} issue.");
        }
        else
            context.UserData.SetValue<string>(ContextConstants.IssueType, sIssueTypeFromUser);
        sIssueTypeFromSession = context.UserData.GetValue<string>(ContextConstants.IssueType).ToLower();
        if (sIssueTypeFromSession.Contains("ie") || sIssueTypeFromSession.Contains("webpage") || sIssueTypeFromSession.Contains("internet explorer"))
        {
            string strMessege = "";
            strMessege = $"The L&D website has multimedia content that may be causing the website to run slowly. Depending on what browser you're using I can either provide a click to fix article to disable the multimedia, or help you create a ticket.";
            PromptDialog.Choice(context, this.OnOptionSelectedIE, new List<string>() { strYes, strNo }, "Are you using I.E. 11?", "Not a valid option", 1);
            context.UserData.SetValue<string>(ContextConstants.IssueType, "");
            await context.PostAsync(strMessege);
        }
    }

    //Handler - IE selection 
    private async Task OnOptionSelectedIE(IDialogContext context, IAwaitable<string> result)
    {
        string optionSelected = await result;
        switch (optionSelected)
        {
            case strYes:
                await context.PostAsync(CreateAttachmentWithHeroCard(context, "", $"I found a click to fix article that might help you resolve the issue. Let me know if you need anything else.", $"Internet Explorer 11 - How to Turn Multimedia Off in Internet Explorer", ActionTypes.OpenUrl, "https://unisysshowcasedemo.service-now.com/uit?id=kb_article&sys_id=76b2f0b70f4e2a005ab1e709b1050e8a"));
                PromptDialog.Choice(context, this.OnOptionSelected1IE, new List<string>() { "Yes", "No" }, "Did my recommendation solve your issue?", "Not a valid option", 1);
                break;

            case strNo:
                await context.PostAsync(CreateAttachmentWithHeroCard(context, "", $"I couldn't find a click to fix article for your browser. Let's get a ticket submitted to the help desk that way we can get the issue resolved for you.", $"Create a Ticket", ActionTypes.OpenUrl, "https://unisysshowcasedemo.service-now.com/uit"));
                PromptDialog.Choice(context, this.OnOptionSelected2IE, new List<string>() { "Yes", "No" }, "Let me know if there is anything else I can help you with.", "Not a valid option", 1);
                break;
        }

    }

    //Handler - Recommendation solved the issue
    private async Task OnOptionSelected1IE(IDialogContext context, IAwaitable<string> result)
    {

        string optionSelected = await result;

        switch (optionSelected)
        {
            case "Yes":
                PromptDialog.Choice(context, this.OnOptionSelected2IE, new List<string>() { "Yes", "No" }, "Let me know if there is anything else I can help you with.", "Not a valid option", 1);
                break;

            case "No":
                await context.PostAsync(CreateAttachmentWithHeroCard(context, "", $"I'm sorry I can't help further. You seem to be looking for something that's outside of my current skill set. I can transfer our conversation to a live chat representative.", $"Connect to Live Chat", ActionTypes.OpenUrl, $"https://www.unisys.com/about-us/support/unisys-support-services"));
                await context.PostAsync($"It was a pleasure speaking with you. Have a Good Day!");
                break;
        }
    }

    //Handler - Anything else to help with
    private async Task OnOptionSelected2IE(IDialogContext context, IAwaitable<string> result)
    {

        string optionSelected = await result;

        switch (optionSelected)
        {
            case "Yes":
                await context.PostAsync($"Please elaborate your issue.");
                break;

            case "No":
                await context.PostAsync($"It was a pleasure speaking with you. Have a Good Day!");
                break;
        }

    }

    public string WelcomeMessage()
    {
        string strMsg = $"Greetings! My name is Iva. I am a Virtual Agent. How may i help you today?";
        return strMsg;
    }

    //Response - Image Attachment
    public IMessageActivity CreateImageAttachment(IDialogContext context, string strText, string strImageName, string strCurrentURL)
    {
        var resultMessage = context.MakeMessage();
        resultMessage.Text = strText;
        resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
        resultMessage.Attachments = new List<Attachment>();

        Attachment attImage1 = new Attachment()
        {
            ContentType = "image/png",
            ContentUrl = String.Format(@"{0}/{1}", strCurrentURL, "unisysazurebotservicestorage/" + strImageName)
        };

        resultMessage.Attachments.Add(attImage1);
        return resultMessage;
    }

    //Response - Hero Card Attachment
    public IMessageActivity CreateAttachmentWithHeroCard(IDialogContext context, string strTitle, string strSubTitle, string strBtnTitle, string btnActionType, string strBtnValue)
    {
        var resultMessage = context.MakeMessage();
        resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
        resultMessage.Attachments = new List<Attachment>();

        HeroCard heroCard = new HeroCard()
        {
            Title = strTitle,
            Subtitle = strSubTitle,
            Buttons = new List<CardAction>()
                        {
                            new CardAction()
                            {
                                Title = strBtnTitle,
                                Type = btnActionType,
                                Value = strBtnValue
                            }
                        }
        };

        resultMessage.Attachments.Add(heroCard.ToAttachment());
        return resultMessage;
    }


}
