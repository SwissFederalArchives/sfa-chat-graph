import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';


@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  standalone: false,
  styleUrl: './app.component.css'
})

export class AppComponent implements OnInit {

  constructor(private http: HttpClient) { }

  ngOnInit(): void {
  }



  title = 'sfa-chat-graph.client';
}
