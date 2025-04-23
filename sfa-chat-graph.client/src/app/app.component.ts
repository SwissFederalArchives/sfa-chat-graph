import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { AfterViewInit, Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Graph } from './graph/graph';
import { filter, firstValueFrom } from 'rxjs';
import { ApiClientService } from './services/api-client/api-client.service';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { ChatHistoryComponent } from './chat-history/chat-history.component';
import { EventChannel } from './services/api-client/event-channel.service';

const DATABASE = "TestDB";
const MAX_LOAD_COUNT: number = 15;

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  standalone: false,
  styleUrl: './app.component.css'
})
export class AppComponent {

  constructor(private router: Router, private route: ActivatedRoute) {
        router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(async (event: NavigationEnd) => {
        if (event.url == "/" || event.url == "/chat" || event.url == "" || event.url == undefined) {
          await router.navigate(["/chat", window.crypto.randomUUID()]);
        }
      });
  }

}