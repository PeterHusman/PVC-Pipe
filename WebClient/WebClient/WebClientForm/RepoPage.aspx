<%@ Page Async="true" Language="C#" AutoEventWireup="true" CodeBehind="RepoPage.aspx.cs" Inherits="WebClientForm.RepoPage" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
        </div>
        <asp:Button ID="returnButton" runat="server" Text="Return to main page" OnClick="returnButton_Click" />
        <p>
            <asp:Label ID="repoLabel" runat="server" Text="Repository name: "></asp:Label>
        </p>
        <asp:Label ID="branchLabel" runat="server" Text="Branches:"></asp:Label>
        <asp:BulletedList ID="branchList" runat="server" OnClick="branchList_Click" DisplayMode="LinkButton">
        </asp:BulletedList>
        <asp:Label ID="commitLabel" runat="server" Text="Commits:"></asp:Label>
        <asp:BulletedList ID="commitList" runat="server" DisplayMode="LinkButton" OnClick="commitList_Click">
        </asp:BulletedList>
        <asp:Label ID="filesLabel" runat="server" Text="Files:"></asp:Label>
        <asp:BulletedList ID="filesList" runat="server">
        </asp:BulletedList>
    </form>
</body>
</html>
