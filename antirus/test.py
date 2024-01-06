import asyncio
from typing import List
import requests
import xml.etree.ElementTree as ET
import json
import os
import sys
import base64
from bs4 import BeautifulSoup

#steam account nationality detection
URI = "https://steamcommunity.com/id/"
URI_ID = "https://steamcommunity.com/profiles/"
STEAMKEY = "" #https://steamcommunity.com/dev/apikey
FRIENDSURI = "https://api.steampowered.com/ISteamUser/GetFriendList/v0001/?key="+STEAMKEY+"&relationship=friend&steamid="


RUSSIAN_GAMES = [
    "War Thunder",
    "World of Tanks Blitz",
    "World of Tanks",
    "World of Warships",
    "World of Warplanes",
    "Crossout",
    "Atomic Heart",
    "Escape from Tarkov",
    "Black Desert",
    "Only Up",
]

RUSSIAN_KEYWORDS = [
    "russia",
    "russki",
    "россия",
    "русск",
    #russian only cyrillic letters 
    "ы",
    "ъ",
    "э",
    "ё"
]
FORGIVE_KEYWORDS = [
    "москаль",
    "кацап",
    "україн",
    "ЗСУ",
    "повернись живим",
    "спільнота",    
    #ukrainian only cyrillic letters
    "ї",
    "є",
    "ґ",
    "і"
]
logs = []

def cachedrequest(url:str) -> str:
    #base64 encode url
    filename = base64.b64encode(url.encode('utf-8')).decode('utf-8').replace('/', '_')
    
    ROOT = os.path.dirname(os.path.abspath(__file__) )+ "/cache/"
    #check if file exists
    if not os.path.exists(ROOT):
        os.makedirs(ROOT)
    #check if file exists
    if os.path.exists(ROOT + filename):
        with open(ROOT + filename, 'r') as f:
            return f.read()
    else:
        r = requests.get(url)
        with open(ROOT + filename, 'w') as f:
            f.write(r.text)
        return r.text
    


def rateWord(word:str) -> bool:
    #check if word contains russian keywords
    for keyword in RUSSIAN_KEYWORDS:
        if keyword in word.lower():
            logs.append(f"знайшли кацапське слово {word}")
            return True
    return False

def forgiveWord(word:str) -> bool:
    #check if word contains ukrainian keywords
    for keyword in FORGIVE_KEYWORDS:
        if keyword in word.lower():
            #logs.append(f"знайшли українське слово {word}")
            return True
    return False

#вертає true якщо русня
def rateText(text:str) -> double:
    #tokenize and check against keywords and forgiveness
    RUS_COEFF = 0.8
    FORGIVE_COEFF = 0.3
    
    SCORE = 0
    #use bs4 to remove all html tags and get text 
    soup = BeautifulSoup(text, 'html.parser')
    words = soup.get_text()
    
    for word in words.split():
        if rateWord(word):
            SCORE += RUS_COEFF
        elif forgiveWord(word):
            SCORE -= FORGIVE_COEFF
    logs.append(f"ранг тексту: {SCORE}")
    return SCORE
    


def tryGet(tree, key) -> str:
    try:
        return tree.find(key).text
    except:
        return ""

class Game:
    name:str
    appid:str
    playtime:str
    is_russian:bool
    def __repr__(self):
        return f"{'RU' if self.is_russian else ''}: {self.name} ({self.appid}) - {self.playtime} hours"
    
    @property
    def playtime_hours(self) -> float:
        if(self.playtime == ""):
            return 0
        return float(self.playtime.replace(',', ''))
    
class Group:
    name:str
    members:int
    description:str
    def __repr__(self):
        return f"{self.name} ({self.members} members)"
    
    @property
    def is_russian(self) -> bool:
        ret = rateText(self.description)
        if ret:
            logs.append(f"знайшли русню в групі {self.name}")
        return ret


    
    
class User:
    steamid: str
    steamid64: str
    name: str
    avatar: str|None
    nationality: str|None
    membersince: str|None
    summary: str|None
    
    games: List[Game]
    groups: List[Group]
    friendsID: List[str]
    friends: List['User']
    
    friendsince: int|None #unix timestamp
    
    scannedRate = None
    
    def is_russian(self, count_friends:bool) -> float:
        if self.scannedRate != None and not count_friends:
            return self.scannedRate
        
        #calculate probability of being russian
        profile_weight = 1.0
        game_weight = 0.8
        group_weight = 0.5
        friends_weight = 0.2
        
        friend_score = 0
        print("Перевіряємо профіль", self.name)
        profile_score = 0 
        #check nationality, if not ukraine then add 1
        if self.nationality != None:
            if "ukraine" in self.nationality.lower():
                profile_score -= 0.2
                print("профіль український")
                self.warns.append("профіль український")
            if "russia" in self.nationality.lower():
                profile_score += 10
                print("профіль російський")
                self.warns.append("профіль російський")
        profile_score *= profile_weight
        #check summary
        if self.summary != None:
            if rateText(self.summary):
                profile_score += 0.5
                print("профіль містить кацапський текст")
                self.warns.append("профіль містить кацапський текст")
            elif forgiveWord(self.summary):
                profile_score -= 0.2
                print("профіль містить український текст")
        
        
        if count_friends and len(self.friends) != 0:
            for friend in self.friends:
                rate = friend.is_russian(False) 
                if rate > 0:
                    friend_score += rate
                    print(f"Знайшли русню {friend.steamid}")
                    self.warns.append(f"Знайшли русню {friend.steamid}")
            #divide by total friends
            friend_score /= len(self.friends)
            print(f"відсоток кацапів у друзях: {friend_score}")
            self.warns.append(f"відсоток кацапів у друзях: {friend_score}")
            #multiply by weight
            friend_score *= friends_weight
        
        game_score = 0
        for game in self.games:
            if game.is_russian:
                game_score += 1
                print(f"Знайшли кацапську гру {game.name}")
                self.warns.append(f"Знайшли кацапську гру {game.name}")
                
        if(len(self.games) != 0):       
            game_score /= len(self.games)
            game_score *= game_weight
        
        group_score = 0
        for group in self.groups:
            if group.is_russian:
                group_score += 1
                print(f"Знайшли кацапську групу {group.name}")
                self.warns.append(f"Знайшли кацапську групу {group.name}")
        if(len(self.groups) != 0):
            group_score /= len(self.groups)
            group_score *= group_weight
            
        self.scannedRate = profile_score + game_score + group_score + friend_score / 4
        self.data = str(profile_score) + " " + str(game_score) + " " + str(group_score) + " " + str(friend_score)
        return self.scannedRate
        
        
        
        
    
    
    def __init__(self, name):
        self.name = name
        self.games = []
        self.groups = []
        self.friendsID = []
        self.friends = []
        self.warns=[]
    def orderGames(self):
        #russian topmost, then hours
        self.games.sort(key=lambda x: (x.is_russian, x.playtime_hours), reverse=True)
        
    def getInfo(self, isId = False):
        req = cachedrequest((URI_ID if isId else URI) + self.name + "/?xml=1")
        #parse xml using xmltodict
        tree = ET.fromstring(req)
        self.steamid64 = tryGet(tree, 'steamID64')
        self.steamid = tryGet(tree, 'steamID')
        self.avatar = tryGet(tree, 'avatarFull')
        self.nationality = tryGet(tree, 'location')
        self.membersince = tryGet(tree, 'memberSince')
        self.summary = tryGet(tree, 'summary')
        
        isClosed = tryGet(tree, 'privacyState')
        if isClosed == "private":
            print("Профіль приватний")
            return
        #groups
        for group in tree.findall('groups/group'):
            g = Group()
            g.name = tryGet(group, 'groupName')
            if(g.name == ""): 
                continue
            g.members = int(tryGet(group, 'memberCount'))
            g.description = tryGet(group, 'summary')
            self.groups.append(g)
            
        #scan thru games
        greq = cachedrequest(URI_ID +(self.steamid64 if self.steamid64 else self.name)+ "/games/?xml=1")
        try:
            gtree = ET.fromstring(greq)
            for game in gtree.findall('games/game'):
                g = Game()
                g.name = tryGet(game, 'name')
                g.appid = tryGet(game, 'appID')
                g.playtime = tryGet(game, 'hoursOnRecord')
                g.is_russian = g.name in RUSSIAN_GAMES
                self.games.append(g)
            self.orderGames()
        except:
            print("не можу знайти ігри")
            return
        
    def loadFriends(self):
        #scan thru friends, its JSON, not XML
        f = cachedrequest(FRIENDSURI + self.steamid64)
        fjson = json.loads(f)
        for friend in fjson['friendslist']['friends']:
            self.friendsID.append(friend['steamid'])
            #create user objects for each friend
            self.friends.append(User(friend['steamid']))
        #get friends info in parallel
        for friend in self.friends:
            friend.getInfo(True)
        #sort friends by russian
        self.friends.sort(key=lambda x: x.is_russian(False), reverse=True)
        
    
            
    def __repr__(self):
        rutex = self.is_russian(False)
        return f"{rutex} {self.steamid} ({self.steamid64}) {self.nationality} {self.membersince} {self.summary}"
            
        
        
        
        

#if __name__ == "__main__":
#    if len(sys.argv) < 2:
#        print("Usage: python3 test.py [username]")
#        exit(0)
user = User("76561198984831363")
user.getInfo(True)
print(user.steamid64)
print(user.steamid)
print(user.avatar)
print(user.nationality)
print(user.membersince)
print(user.summary)

user.loadFriends()
print(user.is_russian(True))

#user.friends[2].loadFriends()
user.friends[2].is_russian(True)
print("кінець")
    
        
        
    