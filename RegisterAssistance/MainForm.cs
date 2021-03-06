using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

using CefSharp;
using CefSharp.WinForms;

using RegisterAssistance.captcha;

namespace RegisterAssistance
{
    public partial class MainForm : Form
    {
        public int index = 0;
        public bool vcode_shown = false;
        public string mailPassword = "";

        public IBrowser browser;
        public ChromiumWebBrowser chromeBrowser_steam, chromeBrowser_mail;

        public List<Account> data = new List<Account>();
        public List<string> avatars = new List<string>();

        private CodeInputDialog currentCaptchaDialog = null;

        public MainForm()
        {
            InitializeComponent();
            FormClosing += (sender,e) => Cef.Shutdown();
            CheckForIllegalCrossThreadCalls = false;
            Cef.Initialize(new CefSettings()
            {
                CachePath = "Cache",
                UserDataPath = "UserData",
                LogSeverity = LogSeverity.Disable
            });
            panel_steam.Controls.Add(chromeBrowser_steam = new ChromiumWebBrowser("")
            {
                Dock = DockStyle.Fill
            });
            chromeBrowser_steam.FrameLoadEnd += (sender,e) =>
            {
                textBox_url.Text = e.Url;
                fillForm();
            };
            chromeBrowser_steam.IsBrowserInitializedChanged += (sender,e) =>
            {
                if(e.IsBrowserInitialized)
                {
                    browser = chromeBrowser_steam.GetBrowser();
                }
            };
            panel_mail.Controls.Add(chromeBrowser_mail = new ChromiumWebBrowser("")
            {
                Dock = DockStyle.Fill
            });
            chromeBrowser_mail.FrameLoadEnd += (sender,e) =>
            {
                chromeBrowser_mail.ExecuteScriptAsync("document.write('');");
            };
            switch(Program.config["CaptchaProcessor",""])
            {
            case "Yundama":
                CodeInputDialog.captchaProcessor = new Yundama();
                break;
            }
        }
        
        private string getAvatar(int id)
        {
            return avatars[id % avatars.Count];
        }

        private void loadUrl(string url)
        {
            if(url == browser.MainFrame.Url)
            {
                chromeBrowser_steam.Reload();
            }
            else
            {
                chromeBrowser_steam.Load(url);
            }
        }

        private void loadAccount()
        {
            button8.Enabled = index < data.Count - 1;
            button9.Enabled = index != 0;
            Text = "Register Assistant - " + data[index].Username;
            if(radioButton_default_login.Checked)
            {
                button7.PerformClick();
            }
            else
            {
                button1.PerformClick();
            }
            Cef.GetGlobalCookieManager().DeleteCookies("http://steamcommunity.com");
            Cef.GetGlobalCookieManager().DeleteCookies("https://store.steampowered.com");
        }

        private void fillForm()
        {
            var data = this.data[index];
            var url = browser.MainFrame.Url.ToLower().Replace("http://","").Replace("https://","").Replace("//","/").TrimEnd(new char[] { '/' });
            Console.WriteLine(url);
            switch(url)
            {
            case "store.steampowered.com/join":
                browser.MainFrame.ExecuteJavaScriptAsync("jQuery('#accountname').val('" + data.Username + "');" +
                    "CheckAccountNameAvailability();" +
                    "jQuery('#password,#reenter_password').val('" + data.Password + "');" +
                    "CheckPasswordStrength();" +
                    "ReenterPasswordChange();" +
                    "jQuery('#email,#reenter_email').val('report_bot_"+data.Id+"@csgo.report');" +
                    "jQuery('#i_agree_check').click();" +
                    "jQuery('.ssa_box').height(10);" +
                    "jQuery('#captcha_text').focus();" +
                    "window.scrollY=400;");
                vcode_shown = false;
                break;
            case "store.steampowered.com/account/registerkey":
                if(!checkBox_auto_cdk.Checked)
                {
                    return;
                }
                browser.MainFrame.ExecuteJavaScriptAsync("jQuery('#product_key').val('" + data.CDK + "');" +
                    "jQuery('#accept_ssa').click();" +
                    "var _lol_hacked=DisplayPage;" +
                    "DisplayPage=function(page)" +
                    "{" +
                        "_lol_hacked(page);" +
                        "if(page=='receipt')" +
                        "{" +
                            (checkBox_auto_go_group.Checked ? "document.location='http://steamcommunity.com/groups/csgo_report/';" : "") +
                        "}" +
                    "};" +
                    "RegisterProductKey();");
                break;
            case "steamcommunity.com/groups/csgo_report":
                if(!checkBox_auto_group.Checked)
                {
                    return;
                }
                browser.MainFrame.ExecuteJavaScriptAsync("if(jQuery('.grouppage_join_area .btn_green_white_innerfade').length==1)" +
                    "{" +
                        "document.forms['join_group_form'].submit();" +
                    "}" +
                    "else if(jQuery('.grouppage_join_area .btn_blue_white_innerfade').length==1)" +
                    "{" +
                        (checkBox_auto_go_2fa.Checked? "document.location='https://store.steampowered.com/twofactor/manage';" : "") +
                    "}");
                break;
            case "store.steampowered.com/twofactor/manage":
                if(!checkBox_auto_disable_steam_guard.Checked)
                {
                    return;
                }
                browser.MainFrame.EvaluateScriptAsync("jQuery('#none_authenticator_check').prop('checked')?'true':'false'").ContinueWith((r) =>
                {
                    if(!r.IsFaulted && r.Result.Success)
                    {
                        if(r.Result.Result.ToString() == "false")
                        {
                            browser.MainFrame.ExecuteJavaScriptAsync("document.forms['none_authenticator_form'].submit();");
                        }
                        else if(checkBox_auto_go_next_account.Checked && button8.Enabled)
                        {
                            button8.PerformClick();
                        }
                    }
                });
                break;
            case "store.steampowered.com/twofactor/manage_action":
                if(!checkBox_auto_disable_steam_guard.Checked)
                {
                    return;
                }
                browser.MainFrame.EvaluateScriptAsync("jQuery('.btnv6_green_white_innerfade').length==1?'true':'false'").ContinueWith((r) =>
                {
                    if(!r.IsFaulted && r.Result.Success )
                    {
                        if(r.Result.Result.ToString() == "true")
                        {
                            browser.MainFrame.ExecuteJavaScriptAsync("document.getElementById('none_authenticator_form').submit();");
                        }
                        else if(checkBox_auto_go_next_account.Checked && button8.Enabled)
                        {
                            button8.PerformClick();
                        }
                    }
                });
                break;
            case "steamcommunity.com/?go_profile":
            case "store.steampowered.com/?created_account=1":
                if(!checkBox_auto_go_profile.Checked)
                {
                    return;
                }
                browser.MainFrame.ExecuteJavaScriptAsync("var shortcut=jQuery('#account_dropdown .popup_menu_item').last().attr('href');" +
                    "document.location=shortcut.indexOf('/id/')!=-1?shortcut+'/edit':shortcut+'/edit?welcomed=1';");
                break;
            default:
                if(url.StartsWith("store.steampowered.com/login"))
                {
                    if(!checkBox_auto_login.Checked)
                    {
                        return;
                    }
                    browser.MainFrame.ExecuteJavaScriptAsync("jQuery('#input_username').val('" + data.Username + "');" +
                        "jQuery('#input_password').val('" + data.Password + "');" +
                        "jQuery('button[type=submit]').click();");
                }
                else if(url.StartsWith("steamcommunity.com/login"))
                {
                    if(!checkBox_auto_login.Checked)
                    {
                        return;
                    }
                    browser.MainFrame.ExecuteJavaScriptAsync("jQuery('#steamAccountName').val('" + data.Username + "');" +
                        "jQuery('#steamPassword').val('" + data.Password + "');" +
                        "jQuery('#SteamLogin').click();");
                }
                else if((url.StartsWith("steamcommunity.com/profiles/") || url.StartsWith("steamcommunity.com/id/")) && (url.EndsWith("/edit?welcomed=1") || url.EndsWith("/edit")))
                {
                    if(!checkBox_auto_avatar.Checked)
                    {
                        return;
                    }
                    browser.MainFrame.ExecuteJavaScriptAsync("if(" + (checkBox_override_profile_check.Checked ? "false)" : "jQuery('#personaName').val()=='CSGO.Report_Bot" + data.Id + "')") +
                        "{" +
                            (checkBox_auto_go_active.Checked ?
                                (checkBox_direct_go_privacy.Checked ? "jQuery('a:contains(My Privacy Settings)')[1].click();" : 
                                (checkBox_direct_go_2fa.Checked? "document.location='https://store.steampowered.com/twofactor/manage';" :"document.location='https://store.steampowered.com/account/registerkey/';")) : "") +
                        "}" +
                        "else" +
                        "{" +
                            "var byteCharacters=atob('" + getAvatar(data.Id) + "');" +
                            "var byteNumbers=new Array(byteCharacters.length);" +
                            "for(var i=0;i<byteCharacters.length;i++)" +
                            "{" +
                                "byteNumbers[i]=byteCharacters.charCodeAt(i);" +
                            "}" +
                            "var data=new FormData(document.getElementById('avatar_upload_form'));" +
                            "data.append('avatar',new Blob([new Uint8Array(byteNumbers)],{type:'image/png'}));" +
                            "jQuery.ajax(" +
                            "{" +
                                "url:'http://steamcommunity.com/actions/FileUploader'," +
                                "data:data," +
                                "type:'POST'," +
                                "contentType:false," +
                                "processData:false," +
                                "cache:false," +
                                "success:function()" +
                                "{" + (checkBox_auto_profile.Checked ?
                                    "jQuery('#personaName').val('CSGO.Report_Bot" + data.Id + "');" +
                                    "jQuery('#customURL').val('csgo_report_" + data.Id + "');" +
                                    "jQuery('button[type=submit]').click();" : "") +
                                "}," +
                                "error:function(e)" +
                                "{" +
                                    "alert(e);" +
                                "}" +
                            "});" +
                        "}");
                }
                else if((url.StartsWith("steamcommunity.com/profiles/") || url.StartsWith("steamcommunity.com/id/")) && (url.EndsWith("/edit/settings")))
                {
                    if(!checkBox_auto_privacy.Checked)
                    {
                        return;
                    }
                    browser.MainFrame.ExecuteJavaScriptAsync("if(jQuery('.ProfilePrivacyDropDown')[2].innerHTML.startsWith('Public'))" +
                        "{" +
                            (checkBox_auto_go_active.Checked ? (checkBox_direct_go_2fa.Checked ? "document.location='https://store.steampowered.com/twofactor/manage';" : "document.location='https://store.steampowered.com/account/registerkey/';") : "") +
                        "}" +
                        "else" +
                        "{" +
                            "jQuery('.ProfilePrivacyDropDown')[2].click();jQuery('.popup_menu_item:contains(Public)').click();" +
                            "____lol=setInterval(function()" +
                            "{" +
                                "if(jQuery('.PrivacySaveNotice.Saved').length!=0)" +
                                "{" +
                                    "clearInterval(____lol);" +
                                    (checkBox_auto_go_active.Checked ? (checkBox_direct_go_2fa.Checked ? "document.location='https://store.steampowered.com/twofactor/manage';" : "document.location='https://store.steampowered.com/account/registerkey/';") : "") +
                                "}" +
                            "},200);" +
                        "}");
                }
                break;
            }
        }

        #region Events

        private void RegisterForm_Load(object sender,EventArgs e)
        {
            if(Directory.Exists("./avatars/"))
            {
                var files = Directory.EnumerateFiles("./avatars/","*.png");
                foreach(var file in files)
                {
                    avatars.Add(Convert.ToBase64String(File.ReadAllBytes(file)));
                }
            }
            if(avatars.Count == 0)
            {
                Close();
            }
            else
            {
                new ParseDialog(this).ShowDialog();
                if(data.Count == 0)
                {
                    Close();
                }
                else
                {
                    loadAccount();
                    if(mailPassword != "")
                    {
                        new MailForm(this).Show();
                    }
                }
            }
        }

        private void checkBox_auto_get_vcode_CheckedChanged(object sender,EventArgs e)
        {
            timer_get_vcode.Enabled = checkBox_auto_get_vcode.Checked;
        }

        private void textBox_url_KeyPress(object sender,KeyPressEventArgs e)
        {
            if(e.KeyChar == '\r')
            {
                loadUrl(textBox_url.Text);
            }
        }

        private void timer_get_vcode_Tick(object sender,EventArgs e)
        {
            if(browser.IsLoading || browser.MainFrame.Url.ToLower().Replace("http://","").Replace("https://","").Replace("//","/") != "store.steampowered.com/join/")
            {
                return;
            }
            if(!vcode_shown)
            {
                browser.MainFrame.EvaluateScriptAsync("if(jQuery('#lmao_canvas').length==0)" +
                    "{" +
                        "jQuery('body').append('<canvas id=lmao_canvas width='+jQuery('#captchaImg').width()+' height='+jQuery('#captchaImg').height()+' />');" +
                    "}" +
                    "var c=jQuery('#lmao_canvas')[0];" +
                    "c.getContext('2d').drawImage(jQuery('#captchaImg')[0],0,0);" +
                    "c.toDataURL();",timeout: TimeSpan.FromMilliseconds(500)).ContinueWith((r) =>
                    {
                        if(r.IsFaulted || !r.Result.Success || vcode_shown)
                        {
                            return;
                        }
                        Invoke(new Action(() =>
                        {
                            try
                            {
                                var image = Image.FromStream(new MemoryStream(Convert.FromBase64String(r.Result.Result.ToString().Split(',')[1])));
                                vcode_shown = true;
                                currentCaptchaDialog = new CodeInputDialog(this,image);
                                if(currentCaptchaDialog.ShowDialog() == DialogResult.OK)
                                {
                                    browser.MainFrame.ExecuteJavaScriptAsync("jQuery('#captcha_text').val('" + currentCaptchaDialog.Result + "');" +
                                         "CreateAccount();");
                                }
                            }
                            catch { }
                        }));
                    });
            }
            else if(CodeInputDialog.captchaProcessor!=null && checkBox_auto_process_vcode.Checked)
            {
                browser.MainFrame.EvaluateScriptAsync("jQuery('#error_display').css('display')!='none'?'true':'false'",timeout: TimeSpan.FromMilliseconds(100)).ContinueWith((r) =>
                    {
                        if(r.IsFaulted || !r.Result.Success || !vcode_shown || currentCaptchaDialog==null || currentCaptchaDialog.Result.Length==0)
                        {
                            return;
                        }
                        if(r.Result.Result.ToString()=="true")
                        {
                            currentCaptchaDialog.Result = "";
                            CodeInputDialog.captchaProcessor.reportError(currentCaptchaDialog.CaptchaIdentifier);
                            button1.PerformClick();
                        }
                    });
            }
        }

        #endregion

        #region Button Events

        private void button1_Click(object sender,EventArgs e)
        {
            loadUrl("https://store.steampowered.com/join/");
        }

        private void button2_Click(object sender,EventArgs e)
        {
            loadUrl("http://steamcommunity.com/?go_profile");
        }

        private void button3_Click(object sender,EventArgs e)
        {
            loadUrl("https://store.steampowered.com/account/registerkey/");
        }

        private void button4_Click(object sender,EventArgs e)
        {
            fillForm();
        }

        private void button5_Click(object sender,EventArgs e)
        {
            chromeBrowser_steam.ShowDevTools();
        }

        private void button6_Click(object sender,EventArgs e)
        {
            loadUrl("https://store.steampowered.com/twofactor/manage");
        }

        private void button7_Click(object sender,EventArgs e)
        {
            loadUrl("https://steamcommunity.com/login/home/?goto=%3Fgo_profile");
        }

        private void button8_Click(object sender,EventArgs e)
        {
            index++;
            loadAccount();
        }

        private void button9_Click(object sender,EventArgs e)
        {
            index--;
            loadAccount();
        }

        private void button10_Click(object sender,EventArgs e)
        {
            loadUrl("http://steamcommunity.com/groups/csgo_report/");
        }

        #endregion
    }
}
