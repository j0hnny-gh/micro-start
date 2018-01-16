#!/bin/bash
docker run --name some-mysql -v /home/j0hnny/Projects/mysql:/var/lib/mysql -p 3306:3306 -e MYSQL_ROOT_PASSWORD=admin -d mysql:8.0
docker container start some-mysql