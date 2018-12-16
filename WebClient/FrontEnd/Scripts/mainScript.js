let welcomeLabel;
let username;
let password;
let loggedIn;
let loginButton;
let authRepos;
let reposDiv;
let reposLabel;

document.body.onload = () => {
    welcomeLabel = document.getElementById("welcomeLabel");
    loginButton = document.getElementById("logInButton");
    reposDiv = document.getElementById("repos");
    reposLabel = document.getElementById("reposLabel");
    if(sessionStorage.getItem("username") === null || sessionStorage.getItem("username") === undefined)
    {
        return;
    }
    else
    {
        username = sessionStorage.getItem("username");
        password = sessionStorage.getItem("password");
        loginButton.value = "Log Out";
        loggedIn = true;
        reposLabel.innerText = "Repositories for which you have write access:";
        authRepos = JSON.parse(sessionStorage.getItem("authRepos"));
        for(let i = 0; i < authRepos.length; i++)
        {
            reposDiv.innerHTML += "<input type=\"button\" value=" + authRepos[i] +" onclick=\"goToRepo(this)\"></input> <div></div>";
        }
        welcomeLabel.innerText = "Welcome, " + username + ",";
    }
};

function logIn(button)
{
    if(loginButton.value === "Log Out")
    {
        sessionStorage.removeItem("username");
        sessionStorage.removeItem("authRepos");
        sessionStorage.removeItem("password");
        window.location.href = "../Pages/main.html";
    }
    else
    {
        window.location.href = "../Pages/Login.html";
    }
}

function goToRepo(button)
{
    window.location.href = "../Pages/repo.html?repo=" + (button.value + "").replace(" ", "%20");
}