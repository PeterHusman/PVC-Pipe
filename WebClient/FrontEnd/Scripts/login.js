let usernameBox;
let passwordBox;
let loginButton;
let request;

document.body.onload = () => {
    usernameBox = document.getElementById("usernameBox");
    passwordBox = document.getElementById("passwordBox");
    loginButton = document.getElementById("loginButton");
};

function login()
{
    let jsonData = {
        "Username": usernameBox.value,
        "Password": passwordBox.value
    };

    request = new XMLHttpRequest();
    request.open("POST", "http://localhost:56854/api/pipe/checkAuth", true);
    request.setRequestHeader("Content-Type", "application/json");
    request.onreadystatechange = () =>{
        if(request.readyState === 4){
            //callback
            if(request.status >= 200 && request.status < 300)
            {
                loginCallBack(request.responseText);
            }
            else
            {
                failedCallBack();
            }
        }
    };

    request.send(JSON.stringify(jsonData));
}

function failedCallBack()
{
    passwordBox.value = "";
    alert("Invalid login information.");
}

function loginCallBack(respString)
{
    sessionStorage["username"] = usernameBox.value;
    sessionStorage["password"] = passwordBox.value;
    sessionStorage["authRepos"] = respString;
    window.location.href = "../Pages/main.html";
}