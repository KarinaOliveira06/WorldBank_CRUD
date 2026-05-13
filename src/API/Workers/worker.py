import pika
import json
import sys
import os
import time
import requests
import google.generativeai as genai
from dotenv import load_dotenv

load_dotenv()
API_KEY = os.getenv("GEMINI_API_KEY")

genai.configure(api_key=API_KEY)
model = genai.GenerativeModel('gemini-2.0-flash')

def generate_concierge_message(transaction_type, original_message):
    prompt = f"""
    You are a virtual 'Financial Concierge' for a modern digital bank called 'World Bank'.
    Act as a friendly, helpful, and human-like assistant.
    
    Below is a cold system notification regarding a transaction.
    Rewrite this message to be sent via SMS/WhatsApp to the customer.
    The message must be:
    - Short (maximum 2 sentences).
    - Friendly and warm.
    - Include 1 or 2 appropriate emojis.
    
    System data:
    - Type: {transaction_type}
    - System message: {original_message}
    
    New humanized message:
    """
    
    try:
        response = model.generate_content(prompt)
        return response.text.strip()
    except Exception as e:
        return f"Error contacting AI: {e}"

def main():
    print("⏳ Starting AI Concierge Worker...")
    
    connection = None
    while not connection:
        try:
            connection = pika.BlockingConnection(pika.ConnectionParameters(host='rabbitmq'))
            print("✅ Successfully connected to RabbitMQ!")
        except pika.exceptions.AMQPConnectionError:
            print("⏳ RabbitMQ is still booting up... waiting 5 seconds before retrying.")
            time.sleep(5)
    # ----------------------------------------------------

    channel = connection.channel()
    queue_name = 'transaction_notifications'
    channel.queue_declare(queue=queue_name, durable=True)

    def callback(ch, method, properties, body):
        data = json.loads(body)
        t_id = data.get('TransactionId')
        t_type = data.get('Type')
        orig_msg = data.get('Message')

        print("\n" + "="*60)
        print(f"📥 1. NEW TRANSACTION RECEIVED (ID: {t_id}):")
        print(f"   [{t_type}] {orig_msg}")
        
        print("🧠 2. PROCESSING WITH ARTIFICIAL INTELLIGENCE...")
        ai_message = generate_concierge_message(t_type, orig_msg)
        
        print("📱 3. MESSAGE READY FOR SENDING:")
        print(f"   💬 {ai_message}")
        print("="*60 + "\n")

        try:
            response = requests.post(
                f"http://api:8080/api/account/transactions/{t_id}/humanize", 
                json=ai_message,
                headers={"Content-Type": "application/json"}
            )
            if response.status_code == 200:
                print(f"✅ Extrato atualizado no Dashboard (ID: {t_id})")
            else:
                print(f"⚠️ Erro ao enviar para a API: {response.status_code}")
        except Exception as e:
            print(f"❌ Falha de sincronização com a API: {e}")

    channel.basic_consume(queue=queue_name, on_message_callback=callback, auto_ack=True)

    print(f" [*] AI Concierge is listening to the '{queue_name}' queue...")
    channel.start_consuming()

if __name__ == '__main__':
    try:
        main()
    except KeyboardInterrupt:
        print("\nWorker stopped.")
        try:
            sys.exit(0)
        except SystemExit:
            os._exit(0)