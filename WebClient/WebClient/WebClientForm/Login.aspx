<%@ Page Async="true" Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="WebClientForm.Login" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
        </div>
        <asp:Label ID="usernameLabel" runat="server" Text="Username: "></asp:Label>
        <asp:TextBox ID="usrnameBox" runat="server"></asp:TextBox>
        <p>
            <asp:Label ID="passwordLabel" runat="server" Text="Password: "></asp:Label>
            <asp:TextBox ID="passwordBox" runat="server" TextMode="Password"></asp:TextBox>
        </p>
        <asp:Button ID="loginButton" runat="server" OnClick="loginButton_Click" Text="Log In" />
        <asp:Label ID="inval" runat="server" Text="Invalid login" Visible="False"></asp:Label>
    </form>
</body>
</html>
