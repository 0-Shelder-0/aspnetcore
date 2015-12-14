import * as ng from 'angular2/angular2';
import * as router from 'angular2/router';
import * as models from '../../../models/models';
import { Http, HTTP_BINDINGS, Headers, Response } from 'angular2/http';
import { AlbumDeletePrompt } from '../album-delete-prompt/album-delete-prompt';
import { FormField } from '../form-field/form-field';
import * as AspNet from 'angular2-aspnet';

@ng.Component({
    selector: 'album-edit'
})
@ng.View({
    templateUrl: './ng-app/components/admin/album-edit/album-edit.html',
    directives: [router.ROUTER_DIRECTIVES, ng.NgIf, ng.NgFor, AlbumDeletePrompt, FormField, ng.FORM_DIRECTIVES]
})
export class AlbumEdit {
    public form: ng.ControlGroup;
    public artists: models.Artist[];
    public genres: models.Genre[];
    public originalAlbum: models.Album;
    public changesSaved: boolean;

    private _http: Http;

    constructor(fb: ng.FormBuilder, http: Http, routeParam: router.RouteParams) {
        this._http = http;

        var albumId = parseInt(routeParam.params['albumId']);
        http.get('/api/albums/' + albumId).subscribe(result => {
            var json = result.json();
            this.originalAlbum = json;
            (<ng.Control>this.form.controls['Title']).updateValue(json.Title);
            (<ng.Control>this.form.controls['Price']).updateValue(json.Price);
            (<ng.Control>this.form.controls['ArtistId']).updateValue(json.ArtistId);
            (<ng.Control>this.form.controls['GenreId']).updateValue(json.GenreId);
            (<ng.Control>this.form.controls['AlbumArtUrl']).updateValue(json.AlbumArtUrl);
        });

        http.get('/api/artists/lookup').subscribe(result => {
            this.artists = result.json();
        });

        http.get('/api/genres/genre-lookup').subscribe(result => {
            this.genres = result.json();
        });

        this.form = fb.group(<any>{
            AlbumId: fb.control(albumId),
            ArtistId: fb.control(0, ng.Validators.required),
            GenreId: fb.control(0, ng.Validators.required),
            Title: fb.control('', ng.Validators.required),
            Price: fb.control('', ng.Validators.compose([ng.Validators.required, AlbumEdit._validatePrice])),
            AlbumArtUrl: fb.control('', ng.Validators.required)
        });
        
        this.form.valueChanges.observer({  
            next: _ => { this.changesSaved = false; }
        });
    }

    public onSubmitModelBased() {
        // Force all fields to show any validation errors even if they haven't been touched
        Object.keys(this.form.controls).forEach(controlName => {
            this.form.controls[controlName].markAsTouched();
        });

        if (this.form.valid) {
            var controls = this.form.controls;
            var albumId = this.originalAlbum.AlbumId;
            
            this._putJson(`/api/albums/${ albumId }/update`, this.form.value).subscribe(response => {
                if (response.status === 200) {
                    this.changesSaved = true;
                } else {
                    AspNet.Validation.showValidationErrors(response, this.form);
                }
            });
        }
    }

    private static _validatePrice(control: ng.Control): { [key: string]: boolean } {
        return /^\d+\.\d+$/.test(control.value) ? null : { Price: true };
    }
    
    // Need feedback on whether this really is the easiest way to PUT some JSON
    private _putJson(url: string, body: any): Subscribable<Response> {
        return this._http.put(url, JSON.stringify(body), {
            headers: new Headers({ 'Content-Type': 'application/json' })
        });
    }
    
    public get formErrors() {
        return Object.keys(this.form.errors || {});
    }
}

// TODO: Figure out what type declaration is provided by Angular/RxJs and use that instead
interface Subscribable<T> {
    subscribe(callback: (response: Response) => void): void;
}
