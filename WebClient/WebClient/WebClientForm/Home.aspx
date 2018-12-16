<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="WebClientForm.Home" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
        </div>
        <asp:Label ID="welcomeLabel" runat="server" Text="Hello!"/>
        <asp:Button ID="loginButton" runat="server" Text="Sign In" OnClick="loginButton_Click"/>
        <div></div>
        <asp:Label ID="reposLabel" runat="server" Text="Repositories: "></asp:Label>
        <asp:DropDownList ID="repos" runat="server" OnSelectedIndexChanged="repos_SelectedIndexChanged" DataTextField="You are not signed in">
        </asp:DropDownList>
        <asp:Button ID="goToRepo" runat="server" Text="Go to Repository" OnClick="goToRepo_Click" />
    </form>
</body>
</html>
