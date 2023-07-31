var server = "https://course.muketang.com";
//var server = "http://192.168.1.2";
//var miniName="睦课伴读";
//var appId="wx3823924b2804b672"

var miniName="海淀老龄大学";
var appId="wxb45082dbda7c55fe";

var indeUrl = "https://live.weixin.qq.com/livemp/broadcast/index";
var roomUrl = "https://live.weixin.qq.com/livemp/broadcast/control/";
var stompClient;
var indexUrlLogined=undefined;
listenBgMsgAndOpenPage();
connect();
var roomIds={};

var connectId;


function connect() {
	loadRoomStatusLatest();
	if(connectId)
	{
		clearTimeout(connectId);	
	}
    var socket = new SockJS(server + "/muke-ws");
    stompClient = Stomp.over(socket);
    stompClient.connect({}, function (frame) {
        log('Connected: ' + frame);
		if(connectId)
		{
			clearTimeout(connectId);	
		}
        stompClient.subscribe('/topic/live', function (msg) {
        var obj = JSON.parse(msg.body);
		if(obj.appId != appId)
		{
			return;
		}
		syncToLocal(obj.liveRoomId, obj.living);
		checkAndSendToPage(obj.liveRoomId, obj.living);
        });
    }, onerror);
	//5s没反应，重新连接
	connectId = setTimeout(checkConnected, 5000);
}

function checkConnected()
{
	log("5s还没连上，重连...");
	if(stompClient)
	{
		stompClient.disconnect();
	}
	connectId=0;
	connect();	
}


function onerror(frame)
{
	if(connectId)
	{
		clearTimeout(connectId);	
		connectId=0;
	}
	setTimeout(function(){
		log("重连...");
		connect();
	}, 1000);
}



function syncToLocal(roomId, living)
{
	var obj = roomIds[roomId];
	if(obj == undefined)
	{
		obj = {status:living};
	}
	obj.status = living;
	roomIds[roomId] =obj;
}

function syncToLocalSetted(roomId, living)
{
	var obj = roomIds[roomId];
	if(obj == undefined)
	{
		obj = {setted:living};
	}
	var old = obj.setted;
	obj.setted = living;
	roomIds[roomId] =obj;

	stompClient.send("/app/live_callback", {}, JSON.stringify({'liveRoomId': roomId, 'living':living, 'appId': appId }));
	if(obj.setted == obj.status && old != obj.setted)
	{
		closeTab(roomId);
	}
}


function checkAndSendToPage(roomId, living)
{
	var obj = roomIds[roomId];
	if(!obj || obj.setted != living)
	{
		syncToPage(roomId, living);
	}
}

//页面已经存在，不需要打开
//状态不存在，也不需要打开
function syncToPage(roomId, living)
{
	var currentRoomUrl = roomUrl + roomId;
	chrome.tabs.query({}, function(tabs)
	{
		var found = false;
		for(var tab of tabs)
		{
			if(tab.url && tab.url.indexOf(currentRoomUrl) >= 0)
			{
				found=true;
				sendMsgToTab(tab, {cmd :"room_status_set", roomId: roomId, living: living, msg:"room_status_set:" + roomId + " living:" + living });
				break;
			}
		}
		if(found)
		{
			return;
		}
		sendIndexPage(roomId, living);
	});
}

function sendIndexPage(roomId, living)
{
	log("send msg to index page room id : " + roomId + " living:" + living);
	chrome.tabs.query({}, function(tabs)
	{
		for(var tab of tabs)
		{
			if(tab.url && tab.url.indexOf(indeUrl) >= 0)
			{
				sendMsgToTab(tab,{cmd:"live_status_index",roomId: roomId, living: living, msg:"首页打开直播间:" + roomId + " 状态:" + living });
			}
		}
	});
}

function sendMsgToTab(tab, msg)
{
	log("发送消息给tab: " + tab.id + " :" + msg.msg);
	chrome.tabs.sendMessage(tab.id, msg, function(response)
	{
		if(!response)
		{
			msg.sendCount = !msg.sendCount ? 1 : msg.sendCount  + 1;
			log("没有收到回复，延迟1秒发送消息, 已发送次数: " + msg.sendCount);
			if(msg.sendCount > 3)
			{
				reopenHomeTab();
				return;
			}
			setTimeout(function(){
				sendMsgToTab(tab, msg);
			}, 1000);
		}
	});
}


function reopenHomeTab()
{
	var lastIndexId = -1;
	chrome.tabs.query({}, function(tabs)
	{
		for(var tab of tabs)
		{
			if(tab.url && tab.url.indexOf(indeUrl) >= 0)
			{
				lastIndexId = tab.id;
				break;
			}
		}
	});
	
	if(indexUrlLogined)
	{
		log("reopen index url : " + indexUrlLogined);
		chrome.tabs.create({url:indexUrlLogined}, function (tab){
			if(lastIndexId >=0)
			{
				chrome.tabs.remove(lastIndexId);
			}
		});
	}

}


//index页负责监听开始和关闭状态来执行对应操作
function listenBgMsgAndOpenPage()
{
	chrome.runtime.onMessage.addListener(function(request, sender, sendResponse)
	{
		log("bg received msg:" + request.msg);
		if(request.cmd == "room_status_get")
		{
			getRoomStatus(request.roomId, sendResponse)
		}else if(request.cmd == "room_status_check")
		{
			checkRoomPageNeedOpen(request.roomId);
			sendResponse({status:"ok"});
		}else if(request.cmd == "room_status_setted")
		{
			sendResponse({status:"ok"});
			syncToLocalSetted(request.roomId, request.living);
		}else if(request.cmd == "login_expired")
		{
			sendResponse({status:"ok"});
			sendLoginExpired();
		}else if(request.cmd == "logined")
		{
			sendResponse({status:"ok"});
			sendLogined();
		}
		else if(request.cmd == "index_page_url")
		{
			indexUrlLogined = request.url;
			sendResponse({status:"ok"});
			
			setTimeout(function(){
				log("延迟2s加载直播间状态");
				loadRoomStatusLatest();
			}, 2000);
		}else if(request.cmd = "login_scancode")
		{
			sendResponse({status:"ok"});
			sendLoginScancode(request.url);
		}
	});
}

function loadRoomStatusLatest()
{
	var timestamp = (new Date()).getTime() - 60 * 1000;
	$.get(server +"/list_room_status?from="+timestamp + "&appId="+appId, function(respond){
			afterRoomStatusListGot(respond.result);
	});
}



function afterRoomStatusListGot(items)
{
	log("直播间状态list:" + items.length);
	for(var item of items)
	{
		log("获取到直播间:" + item.liveRoomId + " to :" + item.living);
		syncToLocal(item.liveRoomId, item.living);
		checkAndSendToPage(item.liveRoomId, item.living);
	}
}

function sendLoginExpired()
{
	$.get(server +"/chrome/login_expired?appId=" + appId , function(respond){
	});
}

function sendLoginScancode(url)
{
	//url = encodeURIComponent(url);
	//$.get(server +"/chrome/login_scancode?appId=" + appId + "&scancode="+url, function(respond){
	//});
	var d = new Date();
	var n = d.getTime();
	var filename  ="live_muke_" + appId + "_" +  n+".png";
	var nstr = '' + n;
	var length = url.length -nstr.length;
	url = url.substr(0,length) + nstr;
	//log("new url:" + url);
	chrome.downloads.download({url:url, filename :filename }, function(downloadId){
		
	});
}

function sendLogined()
{
	$.get(server +"/chrome/logined?appId=" + appId , function(respond){
	});
}


function getRoomStatus(roomId, callback)
{
	var obj = roomIds[roomId];
	if(obj == undefined)
	{
		callback({value:undefined});
		return;
	}
	callback({value:obj.status});
}

function  closeTab(roomId)
{
	var currentRoomUrl = roomUrl + roomId;
	chrome.tabs.query({}, function(tabs)
	{
		var found = false;
		for(var tab of tabs)
		{
			if(tab.url && tab.url.indexOf(currentRoomUrl) >= 0)
			{
				chrome.tabs.remove(tab.id);
				break;
			}
		}
	});
}

function log(msg)
{
	var timestamp =new Date().Format("yyyy-MM-dd hh:mm:ss.S");
	console.log(timestamp + ":" + msg);
}

Date.prototype.Format = function (fmt) { //author: meizz 
    var o = {
        "M+": this.getMonth() + 1, //月份 
        "d+": this.getDate(), //日 
        "h+": this.getHours(), //小时 
        "m+": this.getMinutes(), //分 
        "s+": this.getSeconds(), //秒 
        "q+": Math.floor((this.getMonth() + 3) / 3), //季度 
        "S": this.getMilliseconds() //毫秒 
    };
    if (/(y+)/.test(fmt)) fmt = fmt.replace(RegExp.$1, (this.getFullYear() + "").substr(4 - RegExp.$1.length));
    for (var k in o)
    if (new RegExp("(" + k + ")").test(fmt)) fmt = fmt.replace(RegExp.$1, (RegExp.$1.length == 1) ? (o[k]) : (("00" + o[k]).substr(("" + o[k]).length)));
    return fmt;
}