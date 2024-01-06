import requests
import json 
import os
import csv
import bs4

URL = "https://store.steampowered.com/curator/42985013/"

#open predownloaded json from feed (get it using devtools)
with open('russians.json') as json_file:
    data = json.load(json_file)
    html = data['results_html']
    soup = bs4.BeautifulSoup(html, 'html.parser')
    #print(soup.prettify())
    
    #weare interested in data-ds-appid="1572190" and img's alt for game name
    
    #container <div class="recommendation">
    games = soup.find_all('div', class_='recommendation')
    game_list = []
    for game in games:
        #get a class="store_capsule"
        link = game.find('a', class_='store_capsule')
        #print(link)
        #get game id
        game_id = link.get('data-ds-appid')
        #img inside a - alt is game name
        game_name = link.find('img').get('alt')
        game_list.append((game_id, game_name))
        
                          
    #print(game_list)
    print("Total games: ", len(game_list))
    print("First game: ", game_list[0])
    print("Last game: ", game_list[-1])
    print("Slava Ukraini!")
    
    #write to csv
    with open('russiangames.csv', 'w', newline='') as csvfile:
        writer = csv.writer(csvfile, delimiter=',')
        writer.writerow(['game_id', 'game_name'])
        for game in game_list:
            writer.writerow([game[0], game[1]])