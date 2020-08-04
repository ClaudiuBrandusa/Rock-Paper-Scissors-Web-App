# Rock-Paper-Scissors-Web-App
ASP.NET Core 3.1 MVC SignalR Web Application.

## Showcase
#### Landing page
We just need an username to connect. The rules are simple, you can use any name you want until it's not someone connected with it.  
It's not a secure approach but for now we will let it be.
![alt text](https://github.com/ClaudiuBrandusa/Rock-Paper-Scissors-Web-App/blob/master/images/0.PNG)
#### Picking an username
![alt text](https://github.com/ClaudiuBrandusa/Rock-Paper-Scissors-Web-App/blob/master/images/1.PNG)
#### Game Page
This page is being updated dynamically by javascript. And the javascripts are triggered by the client-server 
interactions powered by SignalR.  
We do have a members list where we can see all of the active users. 
By clicking on one member's name we begin a game with the selected player.
![alt text](https://github.com/ClaudiuBrandusa/Rock-Paper-Scissors-Web-App/blob/master/images/2.PNG)
#### During the game
We have 3 options: Rock, Paper and Scissors. After we select our option we click on ```Choose``` button.
![alt text](https://github.com/ClaudiuBrandusa/Rock-Paper-Scissors-Web-App/blob/master/images/3.PNG)
#### Waiting for response
Now that we chose our option we have to wait for the opponent to choose.
![alt text](https://github.com/ClaudiuBrandusa/Rock-Paper-Scissors-Web-App/blob/master/images/4.PNG)
#### From the opponent's perspective
Keep in mind that you can see the client's username on the top left side of the page.
![alt text](https://github.com/ClaudiuBrandusa/Rock-Paper-Scissors-Web-App/blob/master/images/5.PNG)
#### The results
Now that the opponent chose we know who won and who lose.
![alt text](https://github.com/ClaudiuBrandusa/Rock-Paper-Scissors-Web-App/blob/master/images/6.PNG)
#### What now?
After the game is game is over the server is saving the result in the database.  
![alt text](https://github.com/ClaudiuBrandusa/Rock-Paper-Scissors-Web-App/blob/master/images/7.PNG)

## .NET Packages used
- Microsoft.AspNetCore.Authentication.JwtBearer
- Microsoft.AspNetCore.Mvc.NewtonsoftJson
- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.EntityFrameworkCore.Tools

## Tools used
- Visual Studio 2019

## Database
We are using SQL Server for the database system. For now we keep track for only the User model in the database(which holds 3 columns(Username, Games won, Games lost)).

### Connection string
```
"server=(localdb)\\MSSQLLocalDB;database=RockPaperScissorsDb;Trusted_Connection=true;MultipleActiveResultSets=True"
```
We are using ```MultipleActiveResultSets=True``` because we need to access the database asynchronously.

## Before you leave
Let me know if you find any kind of problem with the project.  
This project is still under developement.  
For further collaborations please inbox me on discord: canfly#5432
