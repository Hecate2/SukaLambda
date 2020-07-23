from gevent import monkey
monkey.patch_all()

import datetime
from mirai import (
    Mirai, Group, Member, Friend,
    GroupMessage, FriendMessage,
    Plain
)
import asyncio

qq = 1578218714 # 字段 qq 的值
authKey = '12345678' # 字段 authKey 的值
mirai_api_http_locate = 'localhost:8080/ws' # httpapi所在主机的地址端口,如果 setting.yml 文件里字段 "enableWebsocket" 的值为 "true" 则需要将 "/" 换成 "/ws", 否则将接收不到消息.

app = Mirai(f"mirai://{mirai_api_http_locate}?authKey={authKey}&qq={qq}")

import interpreter

@app.receiver(GroupMessage)
async def GMHandler(app: Mirai, group: Group, member: Member, message: GroupMessage):
    message = message.toString()
    closure = interpreter.Compiler(message).compile()
    closure()
    # if message.startswith('test'):
    #     print("%r" % (message))
    #     now = datetime.datetime.now()
    #     await app.sendGroupMessage(
    #         group.id,
    #         [
    #             Plain(text=f"{now.strftime('%a, %b %d %H:%M')} !"),
    #         ]
    #     )

if __name__ == "__main__":
    app.run()
