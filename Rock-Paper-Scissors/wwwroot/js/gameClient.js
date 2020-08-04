"use strict";

var connection = new signalR.HubConnectionBuilder()
    .withUrl("/gameHub", {
        accessTokenFactory: () => "testing"
    })
    .build();

// this defines how will the client react when the connection receives a message
connection.on("ListUsers", function (activeUsers) {
    // find the user list
    var list = document.getElementById("list");
    // if there are no other active users
    if (activeUsers.length == 0) {
        // then we have to notice the user that there aren't other players active
        list.innerHTML = "There are no other active players";
        return;
    }
    // clear the users list
    list.innerHTML = "";
    // looping the activeUsers list
    for (var i = 0; i < activeUsers.length; i++) {
        // create the row
        var row = document.createElement("li");
        // stylizing the list item
        row.classList.add("no-list-style");
        // create the anchor
        var anchor = document.createElement("a");
        // setup the event listener
        anchor.addEventListener("click", function () {
            // plays with the selected player
            connection.invoke("PlayWith", anchor.innerText);
            //alert("selected opponent is " + anchor.innerText);
            //this.anchor.classList.remove("link");
        });
        // stylizing the anchor
        anchor.classList.add("link");
        // append anchor to row as a child
        row.appendChild(anchor);
        // assign the row's data
        anchor.innerHTML = activeUsers[i];
        // add the user as a member in the online players list
        list.appendChild(row);
    }
});

connection.on("gameInit", function () {
    var div = document.getElementById("controls");
    
    // setting the choose input
    var select = document.createElement("select");
    select.setAttribute("id", "chooseInput");
    var optionRock = document.createElement('option');
    optionRock.text = 'Rock';
    optionRock.value = 0;
    var optionPaper = document.createElement('option');
    optionPaper.text = 'Paper';
    optionPaper.value = 1;
    var optionScissors = document.createElement('option');
    optionScissors.text = 'Scissors';
    optionScissors.value = 2;
    select.appendChild(optionRock);
    select.appendChild(optionPaper);
    select.appendChild(optionScissors);
    document.getElementById("playerInput").appendChild(select);

    /*var chooseInput = document.createElement("input");
    chooseInput.type = "number";
    chooseInput.setAttribute("id", "chooseInput");
    document.getElementById("playerInput").appendChild(chooseInput);*/

    // setting the choose submit
    var chooseSubmit = document.createElement("button");
    chooseSubmit.type = "button";
    chooseSubmit.innerText = "Choose";
    chooseSubmit.setAttribute("id", "chooseSubmit");
    chooseSubmit.classList.add("button-primary");
    chooseSubmit.onclick = function () {
        //if (!isNaN(chooseInput.value)) { // if the choice is a number
        //alert(parseInt(select.options[select.selectedIndex].value));
        if (!isNaN(parseInt(select.options[select.selectedIndex].value))) {
            if (connection.invoke("Choose", parseInt(select.options[select.selectedIndex].value))) {
                chooseSubmit.parentNode.removeChild(chooseSubmit);
                chooseInput.parentNode.removeChild(chooseInput);
            }
        }
    }
    document.getElementById("playerSubmit").appendChild(chooseSubmit);
    // setting the quit game button
    // we check if we already have a quit button
    var quit = document.getElementById("quitButton");
    if (quit == null) {
        quit = document.createElement("button");
    } // else we create it
    quit.innerHTML = "Quit Game";
    quit.setAttribute("id", "quitButton");
    quit.classList.add("button-primary");
    quit.onclick = function () {
        connection.invoke("EndGame");
    }
    div.appendChild(quit);
});

connection.on("wait", function (status) {
    if (status) {
        document.getElementById("status").innerText = "Waiting...";
    } else {
        document.getElementById("status").innerText = "";
    }
});

connection.on("draw", function () {
    document.getElementById("status").innerText = "It's a draw!";
});

connection.on("win", function () {
    document.getElementById("status").innerText = "You won!";
});

connection.on("loss", function () {
    document.getElementById("status").innerText = "You lose!";
});

connection.on("clearStatusMessage", function () {
    document.getElementById("status").innerText = "";
});

connection.on("setClientsChoice", function (choice) {
    document.getElementById("playersResponse").innerText = choice; 
});

connection.on("setOpponentsChoice", function (choice) {
    document.getElementById("opponentsResponse").innerText = choice;
});

connection.on("initQuitButton", function () {
    // we check if we already have a quit button
    var quit = document.getElementById("quitButton");
    if (quit != null) {
        return;
    } // else we create it
    quit = document.createElement("button");
    quit.innerHTML = "Quit Game";
    quit.setAttribute("id", "quitButton");
    quit.classList.add("button-primary");
    quit.onclick = function () {
        connection.invoke("EndGame");
    }
    document.getElementById("mainGame").appendChild(quit);
});

connection.on("initPlayAgainButton", function (username) {
    var play = document.createElement("button");
    play.innerHTML = "Play Again";
    play.setAttribute("id", "playAgainButton");
    play.classList.add("button-primary");
    play.onclick = function () {
        //alert(username);
        if (!connection.invoke("PlayAgain", username)) { // if we aren't allowed to play again
            // then we quit the game
            var quit = document.getElementById("quitButton");
            if (quit != null) {
                quit.click();
            }
        } else {
            // then we can play
            connection.invoke("PlayWith", username); // the rest of the validation will be done here
        }
        play.parentNode.removeChild(play);
    }
    document.getElementById("controls").appendChild(play);
});

connection.on("resetGame", function () {
    // resetting the responses
    // for client
    var playersResponse = document.getElementById("playersResponse");
    if (playersResponse != null) {
        playersResponse.innerText = "";
    }
    // for the opponent
    var opponentsResponse = document.getElementById("opponentsResponse");
    if (opponentsResponse != null) {
        opponentsResponse.innerHTML = "";
    }
    // resetting the status message
    var status = document.getElementById("status");
    if (status != null) {
        status.innerHTML = "";
    }
    // resetting the play again button
    var play = document.getElementById("playAgainButton");
    if (play != null) {
        play.parentNode.removeChild(play);
    }
});

// this function will remove the input and submit for the choice
connection.on("removeClientForm", function () {
    var chooseSubmit = document.getElementById("chooseSubmit"); 
    if (chooseSubmit != null) {
        chooseSubmit.parentNode.removeChild(chooseSubmit);
    }
    var chooseInput = document.getElementById("chooseInput");
    if (chooseInput != null) {
        chooseInput.parentNode.removeChild(chooseInput);
    }
});

connection.on("endGame", function () {
    //var div = document.getElementById("mainGame");

    // resetting the responses
    // for player
    // input
    var input = document.getElementById("chooseInput");
    if (input != null) {
        input.parentNode.removeChild(input);
    }
    // submit
    var submit = document.getElementById("chooseSubmit");
    if (submit != null) {
        submit.parentNode.removeChild(submit);
    }
    // for client
    var playersResponse = document.getElementById("playersResponse");
    if (playersResponse != null) {
        playersResponse.innerText = "";
    }
    // for the opponent
    var opponentsResponse = document.getElementById("opponentsResponse");
    if (opponentsResponse != null) {
        opponentsResponse.innerHTML = "";
    }
    // resetting the status message
    var status = document.getElementById("status");
    if (status != null) {
        status.innerHTML = "";
    }
    // removing the quit button
    var quit = document.getElementById("quitButton");
    if(quit != null) {
        quit.parentNode.removeChild(quit);
    }
    // removing the play again button
    var play = document.getElementById("playAgainButton");
    if (play != null) {
        play.parentNode.removeChild(play);
    }
});

connection.on("alert", function (status) {
    alert(status);
});

connection.on("UserDisconnected", function (connectionId) {
    connection.invoke("ListUsers")
});

connection.on("UserConnected", function () {
    connection.invoke("ListUsers")
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});