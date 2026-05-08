import pika
import json
import sys
import os
import google.generativeai as genai
from dotenv import load_dotenv

load_dotenv()
API_KEY = os.getenv("GEMINI_API_KEY")

model = genai.GenerativeModel('gemini-1.5-flash')

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

    connection = pika.BlockingConnection(pika.ConnectionParameters(host='localhost'))
    channel = connection.channel()

    queue_name = 'transaction_notifications'
    channel.queue_declare(queue=queue_name, durable=True)

    def callback(ch, method, properties, body):
        system_message = json.loads(body)
        
        transaction_type = system_message.get('Type')
        original_msg = system_message.get('Message')
        
        print("\n" + "="*60)
        print("📥 1. NEW TRANSACTION RECEIVED FROM C#:")
        print(f"   [{transaction_type}] {original_msg}")
        
        print("\n🧠 2. PROCESSING WITH ARTIFICIAL INTELLIGENCE...")
        
        ai_message = generate_concierge_message(transaction_type, original_msg)
        
        print("\n📱 3. MESSAGE READY FOR SENDING (SMS/WHATSAPP):")
        print(f"   💬 {ai_message}")
        print("="*60 + "\n")

    channel.basic_consume(queue=queue_name, on_message_callback=callback, auto_ack=True)

    print(f" [*] AI Concierge started. Listening to the '{queue_name}' queue...")
    print(" [*] Press CTRL+C to exit.")
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