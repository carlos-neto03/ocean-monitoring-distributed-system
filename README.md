# Ocean Monitoring Distributed System

A distributed system designed to simulate ocean monitoring using sensor devices, message queues, and remote services.

## Overview

This project implements a distributed architecture where simulated sensor devices (WAVY) collect ocean data and send it through a messaging system to be processed and analyzed.

The system demonstrates real-world distributed systems concepts such as asynchronous communication, service separation, and scalable data processing.

## Architecture

The system is composed of several components:

- **WAVY Devices** – Simulated sensors that publish ocean data
- **Aggregator** – Subscribes to sensor data and sends it for processing
- **Preprocessing Service** – Normalizes and converts data formats
- **Central Server** – Stores and analyzes processed data
- **Analysis Service** – Performs statistical analysis on collected data

## Communication Technologies

The system uses two main distributed communication paradigms:

### Publish / Subscribe (RabbitMQ)

Used between sensor devices and aggregators to allow asynchronous and scalable data transmission.

### Remote Procedure Calls (gRPC)

Used for communication between services, including preprocessing and analysis services.

## Data Flow

1. WAVY sensors publish ocean data through RabbitMQ.
2. Aggregators subscribe to relevant topics and collect the data.
3. Data is sent to a preprocessing service using gRPC.
4. Processed data is forwarded to the central server.
5. The server communicates with analysis services.
6. Results are stored in a database and made available to the user.

## Technologies

- C#
- RabbitMQ
- gRPC
- JSON
- MongoDB

## Purpose

This project demonstrates distributed systems concepts including:

- Asynchronous messaging
- Service-oriented architecture
- Data processing pipelines
- Distributed communication patterns

## Author

Carlos Neto  
Junior Software Engineer
